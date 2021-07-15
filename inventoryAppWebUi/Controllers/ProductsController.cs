﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using AutoMapper;
using inventoryAppDomain.Entities;
using inventoryAppDomain.Entities.Enums;
using inventoryAppDomain.Services;
using inventoryAppWebUi.Models;

namespace inventoryAppWebUi.Controllers
{
	[Authorize]
	public class ProductsController : Controller
	{
		private readonly IDrugService _drugService;
		private readonly ISupplierService _supplierService;

		public ProductsController(IDrugService drugService, ISupplierService supplierService)
		{
			_drugService = drugService;
			_supplierService = supplierService;

		}
		// GET: Product
		public ActionResult AllProducts()
		{
			return View(_drugService.GetAllDrugs());
		}
		public async Task<ActionResult> AvailableProductsAsync()
		{
			await _drugService.NotifyDrugExpirationAsync();
			var drugs = _drugService.GetAvailableDrugs();

			return View(drugs);
		}
		public ActionResult FilteredProductsList(string searchString)
		{
			var drugs = _drugService.GetAvailableDrugs();
			var drugFilter = _drugService.GetAvailableDrugFilter(searchString);
			if (string.IsNullOrWhiteSpace(searchString) || string.IsNullOrEmpty(searchString))
			{
				var drugsVM = new DrugSearchViewModel
				{
					Drugs = drugs,
					SearchString = searchString
				};
				return View(drugsVM);
			}
			var drugsearchVM = new DrugSearchViewModel
			{
				Drugs = drugFilter,
				SearchString = searchString
			};
			return View(drugsearchVM);
		}


		public ActionResult AddProductForm()
		{
			var drugCategory = new DrugViewModel
			{
				DrugCategory = _drugService.AllCategories()
			};

			return PartialView("_DrugPartial", drugCategory);
		}

		public ActionResult UpdateProduct(int id)
		{

			var drugInDb = Mapper.Map<Drug, DrugViewModel>(_drugService.EditDrug(id));

			if (drugInDb == null) return HttpNotFound("No drug found");

			drugInDb.DrugCategory = _drugService.AllCategories();

			return PartialView("_DrugPartial", drugInDb);
		}

		public ActionResult SaveProduct(DrugViewModel drug)
		{
			if (!ModelState.IsValid)
			{
				drug.DrugCategory = _drugService.AllCategories();
				TempData["failed"] = "failed";
				return PartialView("_DrugPartial", drug);

				// return Json(new { response = "failed"}, JsonRequestBehavior.AllowGet);
			}

			try
			{
				var supplierInDb = _supplierService.GetSupplierWithTag(drug.SupplierTag);

				if (supplierInDb == null)
				{
					//If the supplier tag is not in the Db
					ModelState.AddModelError("SupplierTag", "Supplier Tag isn't registered yet");
					drug.DrugCategory = _drugService.AllCategories();
					TempData["failed"] = "failed";
					return PartialView("_DrugPartial", drug);
				}

				var expiryDate = _drugService.DateComparison(DateTime.Today, drug.ExpiryDate);

				//Add a new drug
				if (drug.Id == 0)
				{
					//DRUG HAS EXPIRED
					if (expiryDate >= 0)
					{
						ModelState.AddModelError("ExpiryDate", "Must be later than today");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					//SUPPLIER IS INACTIVE
					if (supplierInDb.Status == SupplierStatus.InActive)
					{
						ModelState.AddModelError("SupplierTag", "Supplier has been deactivated");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					// DRUG IS NOT GREATER THAN 0
					if (drug.Quantity <= 0)
					{
						ModelState.AddModelError("Quantity", "Quantity should be greater than zero");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					// DRUG PRICE IS LESS THAN 0
					if (drug.Price <= 0)
					{
						ModelState.AddModelError("Price", "Price should be greater than zero");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					var newDrug = Mapper.Map<DrugViewModel, Drug>(drug);
					_drugService.AddDrug(newDrug);

				}
				else
				{
					// update existing drug

					//DRUG HAS EXPIRED
					if (expiryDate >= 0)
					{
						ModelState.AddModelError("ExpiryDate", "Must be later than today");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					//SUPPLIER IS INACTIVE
					if (supplierInDb.Status == SupplierStatus.InActive)
					{
						ModelState.AddModelError("SupplierTag", "Supplier has been deactivated");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					// QUANTITY OF DRUG IS NOT GREATER THAN 0
					if (drug.Quantity <= 0)
					{
						ModelState.AddModelError("Quantity", "Quantity should be greater than zero");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					// DRUG PRICE IS LESS THAN 0
					if (drug.Price <= 0)
					{
						ModelState.AddModelError("Price", "Price should be greater than zero");
						drug.DrugCategory = _drugService.AllCategories();
						TempData["failed"] = "failed";
						return PartialView("_DrugPartial", drug);
					}

					var getDrugInDb = _drugService.EditDrug(drug.Id);

					if (getDrugInDb == null)
						return HttpNotFound("Drug not found!");

					_drugService.UpdateDrug(Mapper.Map(drug, getDrugInDb));
					// return Json(new { response = "success" }, JsonRequestBehavior.AllowGet);

				}
				TempData["added"] = "added";
			}
			catch (Exception ex)
			{

				Console.WriteLine(ex.Message);
				return Json(new { response = ex.Message }, JsonRequestBehavior.AllowGet);

			}
			return Json(new { response = "success" }, JsonRequestBehavior.AllowGet);

		}

		//Get
		[HttpGet]
		public ActionResult AddProductCategory()
		{
			return PartialView("_CategoryPartial");
		}

		//Post
		[HttpPost]
		public ActionResult SaveProductCategory(DrugCategoryViewModel category)
		{
			if (ModelState.IsValid)
			{

				if (string.IsNullOrWhiteSpace(category.CategoryName))
				{
					ModelState.AddModelError("Category Name", @"Please input category");
					//return Json(new { response = "failure", cat = category }, JsonRequestBehavior.AllowGet);

					return PartialView("_CategoryPartial", category);

				}

				var cate = Mapper.Map<DrugCategoryViewModel, DrugCategory>(category);
				_drugService.AddDrugCategory(cate);

				TempData["categoryAdded"] = "added";
				return Json(new { response = "success" }, JsonRequestBehavior.AllowGet);

			}
			TempData["failedToAddCategory"] = "failed";
			return PartialView("_CategoryPartial", category);
		}

		public ActionResult RemoveProduct(int id)
		{
			try
			{
				var drugInDb = _drugService.GetDrugById(id);

				// if the drug is not found
				if (drugInDb == null)
				{
					return HttpNotFound("Drug does not exist");
				}
				_drugService.RemoveDrug(id);

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);

			}
			return RedirectToAction("AllDrugs");

		}

		public ActionResult ListProductCategories()
		{
			return View(_drugService.AllCategories());
		}

		public ActionResult RemoveProductCategory(int id)
		{
			var removeCategory = _drugService.RemoveDrugCategory(id);
			if (!removeCategory)
				return HttpNotFound("Category does not exist");

			_drugService.RemoveDrugCategory(id);
			return RedirectToAction("ListDrugCategories");
		}

		[HttpGet]
		public ActionResult UpdateProductCategory(int id)
		{
			var categoryInDb = Mapper.Map<EditCategoryViewModel>(_drugService.EditDrugCategory(id));

			if (categoryInDb == null) return HttpNotFound("No category found");

			return PartialView("_EditCategoryPartial", categoryInDb);
		}

		[HttpPost]
		public ActionResult UpdateProductCategory(EditCategoryViewModel category)
		{

			_drugService.UpdateDrugCategory(Mapper.Map<DrugCategory>(category));
			return Json(new { response = "success" }, JsonRequestBehavior.AllowGet);

		}

		public ActionResult ViewProduct(int id)
		{
			var drugInDb = Mapper.Map<Drug, DrugViewModel>(_drugService.EditDrug(id));

			if (drugInDb == null) return HttpNotFound("No drug found");

			return PartialView("_ViewDrugPartial", drugInDb);

		}
	}
}