﻿$("#to").keyup(function () { $("#from").val($("#to").val()); });
$("#from").keyup(function () { $("#to").val($("#from").val()); });

function getTotalUnit() {
  document.getElementById("TotalUnit").innerHTML = (document.getElementById("Quantity").value * document.getElementById("UnitPrice").value).toFixed(2);
};
function getTotalRetail() {
  document.getElementById("TotalRetail").innerHTML = (document.getElementById("Quantity").value * document.getElementById("RetailPrice").value).toFixed(2);
};

$(document.body).on('click', '.itemButton', function () {
  var newItem = [];
  var items = document.getElementsByClassName("newItem");
  $.each(items, function () {
    newItem.push($(this).val());
  });

  var orderDetails = [];
  var items = document.getElementsByClassName("itemProperty");
  $.each(items, function () {
    orderDetails.push($(this).val());
  });

  var method = $(this).attr("value");
  var currentPage = parseInt($("#currentPage").text());

  if (($("#orderForm").valid()) || (method == "Cancel")) {
    $.ajax({
      type: "POST",
      data: {
        method: method,
        newItem: newItem,
        orderDetails: orderDetails,
        currentPage: currentPage
      },
      url: "/PurchaseOrder/AddItem",
      success: function (objOperations) {
        $("#orderDetailPartial").html(objOperations);
        rebindValidators();
      }
    });
  }
});

$(document).ready(function showResult() {
  $('#openmodal').trigger('click');
  changePorts();
});

$("#origin").change(function () {
  changePorts();
});

function changePorts() {
  var origin = $("#origin").val();

  var vnPorts = document.getElementsByClassName("vnPorts");
  var hkPorts = document.getElementsByClassName("hkPorts");

  var ports = document.getElementsByClassName("ports");

  var options = [];

  if (origin == "Vietnam") {
    $.each(vnPorts, function () {
      options.push($(this).text());
    });
  }
  else if (origin == "HongKong") {
    $.each(hkPorts, function () {
      options.push($(this).text());
    });
  }

  for (i = 0; i < ports.length; i++) {
    ports[i].options.length = 0;
    if (ports[i].attributes.value != null) {
      var currentValue = ports[i].attributes.value.nodeValue;
    }
    if ((currentValue != null) && (isExist(currentValue, options))) {
      ports[i].options[ports[i].options.length] = new Option(currentValue, currentValue);
    }
    for (j = 0; j < options.length; j++) {
      if ((currentValue == null) || (currentValue != options[j])) {
        ports[i].options[ports[i].options.length] = new Option(options[j], options[j]);
      }
    }
  }
};

function isExist(value, arr) {
  var exist = false;
  for (k = 0; k < arr.length; k++) {
    if (arr[k] == value) {
      exist = true;
      break;
    }
  }

  return exist;
};

$("#resultModal").on({
  'hide.uk.modal': function () {
    window.location.href = "/Order/Display";
  }
});

function rebindValidators() {
  var $form = $("#orderForm");
  $form.unbind();
  $form.data("validator", null);
  $.validator.unobtrusive.parse($form);
  $form.validate($form.data("unobtrusiveValidation").options);
};