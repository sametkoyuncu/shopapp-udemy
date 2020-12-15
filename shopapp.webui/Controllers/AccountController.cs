using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using shopapp.business.Abstract;
using shopapp.webui.EmailServices;
using shopapp.webui.Extensions;
using shopapp.webui.Identity;
using shopapp.webui.Models;

namespace shopapp.webui.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;
        private IEmailSender _emailSender;
        private ICartService _cartService;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender, ICartService cartService)
        {
            _cartService = cartService;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }
        public IActionResult Login(string ReturnUrl=null)
        {
            return View(new LoginModel()
            { 
                ReturnUrl = ReturnUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if(!ModelState.IsValid)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Login Failed",
                    Message = "Lütfen bilgilerinizi kontrol edip tekrar deneyiniz.",
                    AlertType = "danger"
                });
                return View(model);
            }

            // var user = await _userManager.FindByNameAsync(model.UserName);
            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Login Failed",
                    Message = "Eposta veya parola hatalı.",
                    AlertType = "danger"
                });
                return View(model);
            }

            if(!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Login Failed",
                    Message = "Onaylanmamış email hesabı. Lütfen size gönderilen epostadaki bağlantıya tıklayarak hesabınızı onaylayın.",
                    AlertType = "danger"
                });
                return View(model);
            }

            //üçüncü parametre tarayıcı kapanınca silinip silinmemesi
            //4. parametre giriş denemesine bağlı kapanma altif edilsin mi edilmesin mi
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, true, false);

            if(result.Succeeded)
            {
                return Redirect(model.ReturnUrl??"~/");
                // return RedirectToAction("Index", "Home");
            }
            TempData.Put("message", new AlertMessage()
                {
                    Title = "Login Failed",
                    Message = "Eposta veya parola hatalı.",
                    AlertType = "danger"
                });
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user,model.Password);
            if(result.Succeeded)
            {
                //varsayılan kullanıcı rolü
                await _userManager.AddToRoleAsync(user,"customer");
                // generate token
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //user ıd ve token bilgisi ile url oluşturma
                //(get işlemi için paket)
                var url = Url.Action("ConfirmEmail","Account", new {
                    userId = user.Id,
                    token = code
                });
                // email sent
                await _emailSender.SendEmailAsync(model.Email,"ShopApp Hesap Onayı",$"Lütfen email hesabınızı onaylamak için <a href='https://localhost:5001{url}'>buraya</a> tıklayınız");

                return RedirectToAction("Login","Account");
            }
            // ModelState.AddModelError("","Bilinmeyen bir hata oluştu, lütfen tekrar deneyiniz.");
            TempData.Put("message", new AlertMessage()
                {
                    Title = "Register Failed",
                    Message = "Bilinmeyen bir hata oluştu, lütfen tekrar deneyiniz.",
                    AlertType = "danger"
                });
            //bizim diğer yöntem daha iyi
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData.Put("message", new AlertMessage()
            {
                Title = "Logout Success",
                Message = "Hasabınızdan başarılı bir şekilde çıkış yapıldı.",
                AlertType = "warning"
            });
            return Redirect("~/");
        }

        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if(userId==null || token==null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Confirm Email Failed",
                    Message = "Geçersiz istek.",
                    AlertType = "danger"
                });
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if(userId!=null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if(result.Succeeded)
                {
                    //CART OBJESINI OLUSTUR
                    _cartService.InitializeCart(user.Id);
                    TempData.Put("message", new AlertMessage()
                    {
                        Title = "Confirm Email",
                        Message = "Hesabınız onaylandı.",
                        AlertType = "success"
                    });
                    return View();
                }
                else
                {
                    TempData.Put("message", new AlertMessage()
                    {
                        Title = "Confirm Email",
                        Message = "Hesabınız onaylanırken bir hata oluştu.",
                        AlertType = "danger"
                    });
                    return View();
                }
            }
            
            TempData.Put("message", new AlertMessage()
            {
                Title = "Confirm Email",
                Message = "Hesap onaylama işlemi başarısız oldu.",
                AlertType = "danger"
            });
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if(String.IsNullOrEmpty(Email))
            {
                return View();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if(user==null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Forgot Password Error",
                    Message = "Girdiğiniz eposta sisteme kayıtlı değil.",
                    AlertType = "danger"
                });
                return View();
            }
            // generate token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user); 
            //user ıd ve token bilgisi ile url oluşturma //(get işlemi için paket)
            var url = Url.Action("ResetPassword","Account", new {
                userId = user.Id,
                token = code
            });
            // email sent
            await _emailSender.SendEmailAsync(Email,"ShopApp Parola Sıfırlama",$"Parolanızı yenilemek için lütfen <a href='https://localhost:5001{url}'>buraya</a> tıklayınız");

            return View();
        }

        public IActionResult ResetPassword(string userId, string token)
        {
            if(userId == null || token == null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Reset Password Failed",
                    Message = "Hatalı istekde bulundunuz.",
                    AlertType = "danger"
                });
                return RedirectToAction("Index","Home");
            }

            var model = new ResetPasswordModel()
            {
                Token = token
            };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if(!ModelState.IsValid)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Reset Password Error",
                    Message = "Bilgileri kontrol edip tekrar deneyiniz.",
                    AlertType = "danger"
                });
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if(user==null)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Reset Password Failed",
                    Message = "Hatalı istekde bulundunuz.",
                    AlertType = "danger"
                });
                return RedirectToAction("Index","Home");
            }
            
            var result = await _userManager.ResetPasswordAsync(user,model.Token,model.Password);

            if(result.Succeeded)
            {
                TempData.Put("message", new AlertMessage()
                {
                    Title = "Reset Password Success",
                    Message = "İşlem başarılı. Lütfen giriş yapınız.",
                    AlertType = "success"
                });
                return RedirectToAction("Login","Account");
            }
            TempData.Put("message", new AlertMessage()
                {
                    Title = "Reset Password Error",
                    Message = "Bilinmeyen bir hata meydana geldi.",
                    AlertType = "danger"
                });
            return View(model);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}