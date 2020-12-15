using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using shopapp.business.Abstract;
using shopapp.entity;
using shopapp.webui.Extensions;
using shopapp.webui.Identity;
using shopapp.webui.Models;

namespace shopapp.webui.Controllers
{
    [Authorize(Roles="admin")]
    [AutoValidateAntiforgeryToken]
    public class AdminController: Controller
    {
        private IProductService _productService;
        private ICategoryService _categoryService;
        private RoleManager<IdentityRole> _roleManager;
        private UserManager<User> _userManager;
        public AdminController(IProductService productService, 
                               ICategoryService categoryService, 
                               RoleManager<IdentityRole> roleManager,
                               UserManager<User> userManager)
        {
             _productService = productService;
             _categoryService = categoryService;
             _roleManager = roleManager;
             _userManager = userManager;
        }
        /*
            Users
        */
        public IActionResult UserList()
        {
            return View(_userManager.Users);
        }

        public async Task<IActionResult> UserEdit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if(user!=null)
            {
                var selectedRoles = await _userManager.GetRolesAsync(user);
                var roles = _roleManager.Roles.Select(i=>i.Name);

                ViewBag.Roles = roles;
                return View(new UserDetailsModel(){
                    Id = user.Id,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    SelectedRoles = selectedRoles,
                    PreviousUrl = "/admin/user/list"
                });
            }
            return Redirect("~/admin/user/list");
        }
        [HttpPost]
         public async Task<IActionResult> UserEdit(UserDetailsModel model, string[] selectedRoles)
         {
             if(ModelState.IsValid)
             {
                 var user = await _userManager.FindByIdAsync(model.Id);
                 if(user!=null)
                 {
                     user.UserName = model.UserName;
                     user.FirstName = model.FirstName;
                     user.LastName = model.LastName;
                     user.Email = model.Email;
                     user.EmailConfirmed = model.EmailConfirmed;

                     var result = await _userManager.UpdateAsync(user);
                     if(result.Succeeded)
                     {
                        var userRoles = await _userManager.GetRolesAsync(user);
                         //null referance uyarısı almamak için
                         //gelen selectedRoles boşsa boş bir string dizi oluşturuyor 
                        selectedRoles = selectedRoles??new string[]{};
                         //var olan roller ile gelen rolleri karşılaştırıp
                         //zaten seçili olan varsa except ile çıkarılıp
                         //ekleme işlemi yapılıyor
                        await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles).ToArray<string>());
                        await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles).ToArray<string>());
                        
                        TempData.Put("message", new AlertMessage()
                        {
                            Title = "Success",
                            Message = "'" + model.UserName + "' kullanıcısına ait değişiklikler başarı ile kaydedildi.",
                            AlertType = "success"
                        });
                        
                        return Redirect("~/admin/user/list");
                     }
                     // + selectedRoles gönderilmeli, product kısmında yapıldı
                     return View(model);
                 }
                 // + selectedRoles gönderilmeli, product kısmında yapıldı
                 return View(model);
             }
             // + selectedRoles gönderilmeli, product kısmında yapıldı
             return View(model);
         }
        /*
            Roles
        */
        public IActionResult RoleList()
        {
            return View(_roleManager.Roles);
        }
        [HttpGet]
        public IActionResult RoleCreate()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RoleCreate(RoleModel model)
        {
            if(ModelState.IsValid)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
                if(result.Succeeded)
                {
                    return RedirectToAction("RoleList"); 
                }
                else
                {
                    TempData.Put("message", new AlertMessage()
                    {
                        Title = "Role Create Failed",
                        Message = "İşlem yapılırken bir hata meydana geldi. Lütfen tekrar deneyiniz.",
                        AlertType = "warning"
                    });
                    return View(model);
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RoleEdit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            var members = new List<User>();
            var nonmembers = new List<User>();
            foreach (var user in _userManager.Users)
            {
                var list = await _userManager.IsInRoleAsync(user, role.Name)?members:nonmembers;
                list.Add(user);
            }
            var model = new RoleDetails()
            {
                Role = role,
                Members = members,
                NonMembers = nonmembers
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> RoleEdit(RoleEditModel model)
        {
            if(ModelState.IsValid)
            {
                foreach(var userId in model.IdsToAdd ?? new string[]{})
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if(user!=null)
                    {
                        var result = await _userManager.AddToRoleAsync(user, model.RoleName);
                        if(!result.Succeeded)
                        {
                            foreach(var error in result.Errors)
                            {
                                ModelState.AddModelError("",error.Description);
                            }
                        }
                    }
                }

                foreach(var userId in model.IdsToDelete?? new string[]{})
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if(user!=null)
                    {
                        var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);
                        if(!result.Succeeded)
                        {
                            foreach(var error in result.Errors)
                            {
                                ModelState.AddModelError("",error.Description);
                            }
                        }
                    }
                }
                return Redirect("/admin/role/"+model.RoleId);
            }
            return View(model);
        }
        /*

            PRODUCT
        
        */
        public IActionResult ProductList()
        {
            return View(new ProductListViewModel()
            {
                Products = _productService.GetAll()
            });
        }

        [HttpGet]
        public IActionResult ProductCreate()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProductCreate(ProductModel model)
        {
            if(ModelState.IsValid)
            {
                var entity = new Product()
                {
                    Name = model.Name,
                    Url = model.Url,
                    Price = model.Price,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl
                };

                if(_productService.Create(entity))
                {
                    //TempData["message"] = $"{entity.Name} isimli ürün başarı ile eklendi.";
                    CreateMessage($"{entity.Name} isimli ürün başarı ile eklendi.","success");
                    return RedirectToAction("ProductList");
                }
                CreateMessage(_productService.ErrorMessage,"danger");
            }
            return View(model);
            
        }

        [HttpGet]
        public IActionResult ProductEdit(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }

            var entity =_productService.GetByIdWithCategories((int)id);

            if(entity==null)
            {
                return NotFound();
            }
            
            var model = new ProductModel()
            {
                Id = entity.Id,
                Name = entity.Name,
                Url = entity.Url,
                Description = entity.Description,
                ImageUrl = entity.ImageUrl,
                Price = entity.Price,
                IsApproved = entity.IsApproved,
                IsHome = entity.IsHome,
                SelectedCategories = entity.PruductCategories.Select(i=>i.Category).ToList()
            };

            ViewBag.Categories = _categoryService.GetAll();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProductEdit(ProductModel model, int[] categoryIds, IFormFile file)
        {
            if(ModelState.IsValid)
            {
                var entity =_productService.GetById(model.Id);

                if(entity==null)
                {
                    return NotFound();
                }

                entity.Name = model.Name;
                entity.Url = model.Url;
                entity.Description = model.Description;
                entity.Price = model.Price;
                entity.IsApproved = model.IsApproved;
                entity.IsHome = model.IsHome;

                if(file!=null)
                {
                    var extension = Path.GetExtension(file.FileName);
                    //Datetime.Now.Ticks yerine Guid.NewGuid() de olabilir, benzersiz uzun bir string
                    var randomName = string.Format($"{Guid.NewGuid()}{extension}");
                    entity.ImageUrl = randomName;
                    var path = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot\\img",randomName);

                    using(var stream = new FileStream(path,FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                if(_productService.Update(entity, categoryIds))
                {
                    CreateMessage($"{entity.Name} isimli ürün başarı ile güncellendi.","success");
                    return RedirectToAction("ProductList");
                }
                CreateMessage(_productService.ErrorMessage,"danger");
            }
            ViewBag.Categories = _categoryService.GetAll();
            return View(model);
            
        }

        public IActionResult DeleteProduct(int productId)
        {
            var entity = _productService.GetById(productId);

            if(entity==null)
            {
                return NotFound();
            }

            _productService.Delete(entity);
            
            var msg = new AlertMessage()
            {
                Message = $"{entity.Name} isimli ürün başarı ile silindi.",
                AlertType = "danger"
            };
            TempData["message"] = JsonConvert.SerializeObject(msg);
            
            return RedirectToAction("ProductList");
        }

        /*

            CATEGORY
        
        */
        public IActionResult CategoryList()
        {
            return View(new CategoryListViewModel()
            {
                Categories = _categoryService.GetAll()
            });
        }

        [HttpGet]
        public IActionResult CategoryCreate()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CategoryCreate(CategoryModel model)
        {
            if(ModelState.IsValid)
            {
                var entity = new Category()
                {
                    Name = model.Name,
                    Url = model.Url
                };
                _categoryService.Create(entity);

                //TempData["message"] = $"{entity.Name} isimli ürün başarı ile eklendi.";
                var msg = new AlertMessage()
                {
                    Message = $"{entity.Name} isimli kategori başarı ile eklendi.",
                    AlertType = "success"
                };
                TempData["message"] = JsonConvert.SerializeObject(msg);
                return RedirectToAction("CategoryList");
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult CategoryEdit(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }

            var entity =_categoryService.GetByIdWithProducts((int)id);

            if(entity==null)
            {
                return NotFound();
            }
            
            var model = new CategoryModel()
            {
                Id = entity.Id,
                Name = entity.Name,
                Url = entity.Url,
                Products = entity.PruductCategories.Select(p=>p.Product).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult CategoryEdit(CategoryModel model)
        {
            if(ModelState.IsValid)
            {
                var entity =_categoryService.GetById(model.Id);

                if(entity==null)
                {
                    return NotFound();
                }

                entity.Name = model.Name;
                entity.Url = model.Url;

                _categoryService.Update(entity);

                var msg = new AlertMessage()
                {
                    Message = $"{entity.Name} isimli kategori başarı ile güncellendi.",
                    AlertType = "success"
                };
                TempData["message"] = JsonConvert.SerializeObject(msg);
                return RedirectToAction("CategoryList");
            }
            return View(model);
        }
        public IActionResult DeleteCategory(int categoryId)
        {
            var entity = _categoryService.GetById(categoryId);

            if(entity==null)
            {
                return NotFound();
            }

            _categoryService.Delete(entity);
            
            var msg = new AlertMessage()
            {
                Message = $"{entity.Name} isimli kategori başarı ile silindi.",
                AlertType = "danger"
            };
            TempData["message"] = JsonConvert.SerializeObject(msg);
            
            return RedirectToAction("CategoryList");
        }

        [HttpPost]
        public IActionResult DeleteFromCategory(int productId, int categoryId)
        {
            _categoryService.DeleteFromCategory(productId, categoryId);
            return Redirect("/admin/categories/"+categoryId);
        }
        
        private void CreateMessage(string message, string alerttype)
        {
            var msg = new AlertMessage()
            {
                Message = message,
                AlertType = alerttype
            };
            TempData["message"] = JsonConvert.SerializeObject(msg);
        }
    }
}