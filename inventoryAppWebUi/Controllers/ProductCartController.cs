﻿using inventoryAppDomain.Services;
using inventoryAppWebUi.Models;
using System.Web.Mvc;
using inventoryAppDomain.Entities.Enums;
using Microsoft.AspNet.Identity;
using System;
using AutoMapper;
using System.Collections.Generic;
using inventoryAppDomain.Entities;

namespace inventoryAppWebUi.Controllers
{
    public class ProductCartController : Controller
    {
        private readonly IDrugCartService _drugCartService;
        private readonly IDrugService _drugService;

        public ProductCartController(IDrugCartService drugCartService, IDrugService drugService)
        {
            _drugCartService = drugCartService;
            _drugService = drugService;
        }

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var drugCartCountTotal = _drugCartService.GetDrugCartTotalCount(userId);
            var drugCartViewModel = new DrugCartViewModel
            {
                CartItems = _drugCartService.GetDrugCartItems(userId, CartStatus.ACTIVE),
                DrugCartItemsTotal = drugCartCountTotal,
                DrugCartTotal = _drugCartService.GetDrugCartSumTotal(userId),

            };
            return View(drugCartViewModel);
        }
        public ActionResult GetProduct(int id)
        {
            var drug = _drugCartService.GetDrugById(id);
            return View(drug);
        }

        public ActionResult AddToShoppingCart(int id)
        {
            var userId = User.Identity.GetUserId();
            var selectedDrug = _drugCartService.GetDrugById(id);

        //    var prescribeVM = Mapper.Map<DrugPrescriptionViewModel>(selectedDrug);
            try
            {
                if (selectedDrug == null)
                {
                    return HttpNotFound();
                }

                _drugCartService.AddToCart(selectedDrug, userId);
                return RedirectToAction("FilteredDrugsList", "Drug");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return RedirectToAction("FilteredDrugsList", "Drug");
            }
         
        }


        public ActionResult RemoveFromShoppingCart(int id)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var cartItem = _drugCartService.GetDrugCartItemById(id);
                var selectedItem = _drugCartService.GetDrugById(cartItem.Drug.Id);

                if (selectedItem != null)
                {
                    _drugCartService.RemoveFromCart(selectedItem, userId);
                }

                return RedirectToAction("Index");
            }
            catch(Exception e)
            {
                ViewBag.Error = e.Message;
                return RedirectToAction("Index", "DrugCart");
            }
        }

        public ActionResult RemoveAllCart()
        {
            var userId = User.Identity.GetUserId();
            _drugCartService.ClearCart(userId);
            return RedirectToAction("Index");
        }
    }
}
