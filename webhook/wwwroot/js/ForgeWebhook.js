/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

function WebhookNodeSelected(node) {
  if (node.type !== 'folders') return;
  updateCurrentMonitor(node);
}

function updateCurrentMonitor(node) {
  $('#startMonitorFolder').hide();
  $('#stopMonitorFolder').hide();

  var hubHref = node.parents[node.parents.length - 2];
  jQuery.ajax({
    url: '/api/forge/webhook?folder=' + node.id + '&hub=' + hubHref,
    success: function (res) {
      if (res.length == 0)
        $('#startMonitorFolder').show();
      else
        $('#stopMonitorFolder').show();
    }
  });
}

$(document).ready(function () {
  $('#startMonitorFolder').hide();
  $('#stopMonitorFolder').hide();

  $('#startMonitorFolder').click(function () {
    var node = $("#userHubs").jstree("get_selected", true);
    if (node.length != 1 || node[0].type !== 'folders') return;
    var hub = node[0].parents[node[0].parents.length - 2];
    $.ajax({
      type: "POST",
      url: '/api/forge/webhook',
      data: { folder: node[0].id, hub: hub },
      success: function (res) {
        updateCurrentMonitor(node[0]);
      }
    });
  });

  $('#stopMonitorFolder').click(function () {
    var node = $("#userHubs").jstree("get_selected", true);
    if (node.length != 1 || node[0].type !== 'folders') return;
    var hub = node[0].parents[node[0].parents.length - 2];
    $.ajax({
      type: "DELETE",
      url: '/api/forge/webhook',
      data: { folder: node[0].id, hub: hub },
      success: function (res) {
        updateCurrentMonitor(node[0]);
      }
    });
  });
});