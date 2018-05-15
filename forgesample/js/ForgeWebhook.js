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
  $('#monitorFolder').hide();
  if (node.type !== 'folders') return;

  jQuery.ajax({
    url: '/api/forge/webhook?href=' + node.id,
    success: function (res) {
      if (res.length == 0) {
        $('#monitorFolder').show();
      }
    }
  });
}

$(document).ready(function () {
  $('#monitorFolder').click(function () {
    var nodeId = $("#userHubs").jstree("get_selected");
    $.ajax({
      type: "POST",
      url: '/api/forge/webhook',
      data: { href: nodeId },
      success: function (res) {
        console.log(res);
      }
    });
  });
});