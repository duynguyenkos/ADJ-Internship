using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADJ.BusinessService.Dtos;
using ADJ.BusinessService.Interfaces;
using ADJ.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
  public class ManifestController : Controller
  {
    private readonly IManifestService _manifestService;
    public ManifestController(IManifestService manifestService)
    {
      _manifestService = manifestService;
    }
    // GET: Manifest
    public async Task<ActionResult> Index(int? pageIndex, string DestinationPort = null, string OriginPort = null, string Carrier = null, DateTime? ETDFrom = null, DateTime? ETDTo = null, string Status = null, string Vendor = null, string PONumber = null, string Item = null, bool? checkClick = null)
    {
      ViewBag.Size = new List<string> { "20GP", "40HC" };
      ViewBag.PackType = new List<string> { "Boxed", "Carton" };
      ViewBag.Loading = new List<string> { "ROAD" };
      SearchingManifestItem searchItem = await _manifestService.SearchItem();
      ViewBag.OriginPorts = searchItem.OriginPorts;
      ViewBag.Carriers = searchItem.Carriers;
      ViewBag.Dest = searchItem.DestinationPort;
      ViewBag.Status = searchItem.Status;
      int current = pageIndex ?? 1;
      if (String.IsNullOrEmpty(DestinationPort) && String.IsNullOrEmpty(OriginPort) && String.IsNullOrEmpty(Carrier))
      {
        DestinationPort = searchItem.DestinationPort.First();
        OriginPort = searchItem.OriginPorts.First();
        Carrier = searchItem.Carriers.First();
      }
      ViewBag.pageIndex = current;
      PagedListResult<ShipmentManifestsDtos> listManifest = await _manifestService.ListManifestDtoAsync(current, 2, DestinationPort, OriginPort, Carrier, ETDFrom, ETDTo, Status, Vendor, PONumber, Item);
      if (checkClick == true)
      {
        return PartialView("_SearchingManifestPartial", listManifest);
      }
      return View("Index", listManifest);
    }
    [HttpPost]
    public async Task<ActionResult> CreateOrUpdate(PagedListResult<ShipmentManifestsDtos> shipmentManifestDtos)
    {
      ViewBag.modalResult = null;

      if (ModelState.IsValid)
      {
        foreach (var manifest in shipmentManifestDtos.Items)
        {
          if (manifest.selectedContainer == true || manifest.Manifests.Where(p => p.selectedItem == true).Count() > 0)
          {
           await _manifestService.CreateOrUpdateContainerAsync(manifest);
            ViewBag.modalResult = "success";
          }
        }
      }
      else
      {
        if (shipmentManifestDtos.Items.Where(p => p.Manifests.Where(x => x.GrossWeight > x.NetWeight).Count() > 0).Count() > 0)
        {
          ViewBag.modalResult = "grossWeightInvalid";
        }
        else
        ViewBag.modalResult = "invalid";
      }
      ViewBag.Size = new List<string> { "20GP", "40HC" };
      ViewBag.PackType = new List<string> { "Boxed", "Carton" };
      ViewBag.Loading = new List<string> { "ROAD" };
      SearchingManifestItem searchItem = await _manifestService.SearchItem();
      ViewBag.OriginPorts = searchItem.OriginPorts;
      ViewBag.Carriers = searchItem.Carriers;
      ViewBag.Dest = searchItem.DestinationPort;
      ViewBag.Status = searchItem.Status;
      string DestinationPort = searchItem.DestinationPort.First();
      string OriginPort = searchItem.OriginPorts.First();
      string Carrier = searchItem.Carriers.First();
      PagedListResult<ShipmentManifestsDtos> pagedListResult = await _manifestService.ListManifestDtoAsync(1,2,DestinationPort,OriginPort,Carrier);
      return View("Index", pagedListResult);
    }
  }
}