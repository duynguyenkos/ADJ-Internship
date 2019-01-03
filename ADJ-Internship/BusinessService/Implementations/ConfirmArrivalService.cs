﻿using ADJ.BusinessService.Core;
using ADJ.BusinessService.Dtos;
using ADJ.BusinessService.Interfaces;
using ADJ.Common;
using ADJ.DataModel.OrderTrack;
using ADJ.DataModel.ShipmentTrack;
using ADJ.Repository.Core;
using ADJ.Repository.Interfaces;
using AutoMapper;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ADJ.BusinessService.Implementations
{
  public class ConfirmArrivalService : ServiceBase, IConfirmArrivalService
  {
    private readonly IDataProvider<Container> _containerDataProvider;
    private readonly IDataProvider<Booking> _bookingDataProvider;
    private readonly IDataProvider<Order> _orderDataProvider;

    private readonly IContainerRepository _containerRepository;
    private readonly IShipmentBookingRepository _bookingRepository;
    private readonly IConfirmArrivalRepository _confirmArrivalRepository;

    private readonly int pageSize;

    public ConfirmArrivalService(IUnitOfWork unitOfWork, IMapper mapper, ApplicationContext appContext,
      IDataProvider<Container> containerDataProvider, IDataProvider<Booking> bookingDataProvider, IDataProvider<Order> orderDataProvider,
    IContainerRepository containerRepository, IShipmentBookingRepository bookingRepository, IConfirmArrivalRepository confirmArrivalRepository) : base(unitOfWork, mapper, appContext)
    {
      _containerDataProvider = containerDataProvider;
      _bookingDataProvider = bookingDataProvider;
      _orderDataProvider = orderDataProvider;

      _containerRepository = containerRepository;
      _bookingRepository = bookingRepository;
      _confirmArrivalRepository = confirmArrivalRepository;

      pageSize = 6;
    }

    public async Task<PagedListResult<ConfirmArrivalResultDtos>> ListContainerFilterAsync(int? page, DateTime? ETAFrom, DateTime? ETATo, string origin = null, string mode = null,
      string vendor = null, string container = null, string status = null)
    {
      if (page == null) { page = 1; }

      Expression<Func<Container, bool>> All = x => x.Id > 0;

      if (origin != null)
      {
        Expression<Func<Container, bool>> filter = x => x.Manifests.Where(p => p.Booking.Order.Origin == origin).Count() > 0;
        All = All.And(filter);
      }

      if (mode != null)
      {
        Expression<Func<Container, bool>> filter = x => x.Loading == mode;
        All = All.And(filter);
      }

      if (vendor != null)
      {
        Expression<Func<Container, bool>> filter = x => x.Manifests.Where(p => p.Booking.Order.Vendor == vendor).Count() > 0;
        All = All.And(filter);
      }

      if (container != null)
      {
        Expression<Func<Container, bool>> filter = x => x.Name == container;
        All = All.And(filter);
      }

      if ((ETAFrom != null) || (ETATo != null))
      {
        status = ContainerStatus.Despatch.ToString();

        if (ETAFrom != null)
        {
          Expression<Func<Container, bool>> filter = x => x.Manifests.Where(p => p.Booking.ETA >= ETAFrom).Count() > 0;
          All = All.And(filter);
        }

        if (ETATo != null)
        {
          Expression<Func<Container, bool>> filter = x => x.Manifests.Where(p => p.Booking.ETA <= ETATo).Count() > 0;
          All = All.And(filter);
        }
      }

      if (status != null)
      {
        Expression<Func<Container, bool>> filter = x => x.Status.ToString() == status;
        All = All.And(filter);
      }
      else
      {
        Expression<Func<Container, bool>> All1 = x => x.Status == ContainerStatus.Despatch;
        All1 = All.And(All1);
        Expression<Func<Container, bool>> All2 = x => x.Status == ContainerStatus.Arrived;
        All2 = All.And(All2);

        All = All1.Or(All2);
      }

      Expression<Func<Container, bool>> test = x => x.Id > 0;

      PagedListResult<Container> result = await _containerDataProvider.ListAsync(All, null, true, page, pageSize);
      PagedListResult<ConfirmArrivalResultDtos> rs = new PagedListResult<ConfirmArrivalResultDtos>();
      rs.Items = await ConvertToResultAsync(result.Items);
      rs.PageCount = result.PageCount;
      rs.TotalCount = result.TotalCount;

      return rs;
    }

    public async Task<List<ConfirmArrivalResultDtos>> ConvertToResultAsync(List<Container> containers)
    {
      List<ConfirmArrivalResultDtos> result = new List<ConfirmArrivalResultDtos>();

      foreach (var item in containers)
      {
        ConfirmArrivalResultDtos output = new ConfirmArrivalResultDtos();
        Booking booking = await _bookingDataProvider.GetByIdAsync((item.Manifests.ToList())[0].BookingId);
        Order order = await _orderDataProvider.GetByIdAsync(booking.OrderId);

        output.DestinationPort = booking.PortOfDelivery;
        output.Origin = order.Origin;
        output.Mode = item.Loading;
        output.Carrier = booking.Carrier;
        output.ArrivalDate = booking.ETA;

        output.Vendor = order.Vendor;
        output.Container = item.Name;
        output.Status = item.Status;

        output.Id = item.Id;

        var test = typeof(ConfirmArrivalResultDtos).GetProperties()[0].GetValue(output);


        result.Add(output);
      }

      return Sort(result);
    }

    private List<ConfirmArrivalResultDtos> Sort(List<ConfirmArrivalResultDtos> input)
    {
      //Order by Destination Port (property number = 0)
      quickSort(input, 0, input.Count - 1, 0);

      //Order by Origin (property number = 1)
      //Order by Mode (property number = 2)
      //Order by Carrier (property number = 3)
      //Order by Arrival Date (property number = 4)
      for (int property = 1; property < 5; property++)
      {
        int start = 0;
        for (int i = 0; i < input.Count; i++)
        {
          if ((i + 1 < input.Count) || (i == input.Count - 1))
          {
            bool different = false;
            if (i != input.Count - 1)
            {
              for (int j = property - 1; j >= 0; j--)
              {
                var currentProperty = typeof(ConfirmArrivalResultDtos).GetProperties()[j];
                if (currentProperty.GetValue(input[i]).ToString().CompareTo(currentProperty.GetValue(input[i + 1]).ToString()) != 0)
                {
                  different = true;
                  break;
                }
              }
            }

            if ((i == input.Count - 1) || (different))
            {
              quickSort(input, start, i, property);
              start = i + 1;
            }
          }
        }
      }

      return input;
    }

    public static void quickSort(List<ConfirmArrivalResultDtos> A, int left, int right, int property)
    {
      if (left > right || left < 0 || right < 0) return;

      int index = partition(A, left, right, property);

      if (index != -1)
      {
        quickSort(A, left, index - 1, property);
        quickSort(A, index + 1, right, property);
      }
    }

    private static int partition(List<ConfirmArrivalResultDtos> A, int left, int right, int property)
    {
      if (left > right) return -1;

      int end = left;

      if (property != 4)
      {
        var currentProperty = typeof(ConfirmArrivalResultDtos).GetProperties()[property];
        string pivot = currentProperty.GetValue(A[right]).ToString();    // choose last one to pivot, easy to code
        for (int i = left; i < right; i++)
        {
          if (currentProperty.GetValue(A[i]).ToString().CompareTo(pivot) < 0)
          {
            swap(A, i, end);
            end++;
          }
        }
      }
      else
      {
        DateTime pivot = A[right].ArrivalDate;    // choose last one to pivot, easy to code
        for (int i = left; i < right; i++)
        {
          if (A[i].ArrivalDate.CompareTo(pivot) < 0)
          {
            swap(A, i, end);
            end++;
          }
        }
      }

      swap(A, end, right);

      return end;
    }

    private static void swap(List<ConfirmArrivalResultDtos> A, int left, int right)
    {
      var tmp = A[left];
      A[left] = A[right];
      A[right] = tmp;
    }

    public async Task<ConfirmArrivalDtos> CreateOrUpdateCAAsync(int containerId, DateTime arrivalDate)
    {
      CA entity = await GetCAbyContainerId(containerId);
      ConfirmArrivalDtos input = new ConfirmArrivalDtos();

      input.ContainerId = containerId;
      input.ArrivalDate = arrivalDate;

      if (entity != null)
      {
        entity.ArrivalDate = input.ArrivalDate;

        _confirmArrivalRepository.Update(entity);
      }
      else
      {
        entity = Mapper.Map<CA>(input);

        _confirmArrivalRepository.Insert(entity);
      }

      await UpdateContainer(containerId, arrivalDate);

      await UnitOfWork.SaveChangesAsync();

      return Mapper.Map<ConfirmArrivalDtos>(entity);
    }

    public async Task<CA> GetCAbyContainerId(int containerId)
    {
      List<CA> findCA = await _confirmArrivalRepository.Query(x => x.ContainerId == containerId, false).SelectAsync();

      if (findCA.Count != 0)
      {
        return findCA[0];
      }
      else
      {
        return null;
      }
    }

    private async Task<Container> UpdateContainer(int containerId, DateTime arrivalDate)
    {
      Container container = await _containerDataProvider.GetByIdAsync(containerId);
      Booking booking = await _bookingDataProvider.GetByIdAsync((container.Manifests.ToList())[0].BookingId);

      booking.ETA = arrivalDate;
      _bookingRepository.Update(booking);

      container.Status = ContainerStatus.Arrived;
      _containerRepository.Update(container);

      return container;
    }
  }
}