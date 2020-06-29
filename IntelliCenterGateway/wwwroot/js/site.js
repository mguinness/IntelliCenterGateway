// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function reqParam(btn, objnames, keys) {
    $(btn).addClass('disabled');

    if (objnames === undefined) {
        var card = window.event.target.closest('div');
        objnames = $(card).find('tbody > tr').map(function () {
            return $(this).attr('id');
        });
    }

    var objlist = objnames.map(function () {
        return { "objnam": this, "keys": keys };
    });

    var cmd = { "command": "RequestParamList", "objectList": objlist.get(), "messageID": uuidv4() };
    console.log(cmd);
    connection.invoke('Request', JSON.stringify(cmd));
}

function setParam(btn, objname, key, value) {
    var cmd = { "command": "SetParamList", "objectList": [{ "objnam": objname, "params": { [key]: value } }], "messageID": uuidv4() };
    console.log(cmd);
    connection.invoke('Request', JSON.stringify(cmd));
}

function processMsg(msg) {
    if (msg.command === 'SendQuery' && msg.queryName === 'GetHardwareDefinition') {
        if (msg.messageID === configId)
            createCards(msg.answer[0]);
    }
    else if (msg.command === 'NotifyList') {
        console.log(msg);

        msg.objectList.forEach(function (obj) {
            for (var key in obj.params) {
                if (key === 'STATUS')
                    $('#' + obj.objnam).find('td:eq(1)').text(obj.params[key]);
                else if (key === 'PROBE')
                    $('#' + obj.objnam).find('td:eq(1)').text(obj.params[key] + '°');
                else if (key === 'HTMODE')
                    $('#' + obj.objnam).find('td:eq(1)').text(obj.params[key]);
            }
        });
    }
    else if (msg.command === 'SetParamList') {
        console.log(msg);
    } else {
        $("#output").append(JSON.stringify(msg) + '<br />');
    }
}

function createCards(data) {
    var sensors = $.grep(data.params.OBJLIST, function (elem, idx) {
        return elem.params.OBJTYP === 'SENSE';
    });

    var sensorCard = '<div class="card m-1">'
        + '<h5 class="card-header bg-info">Sensors<small class="float-sm-right">'
        + '<a href="#!" class="btn btn-link p-0" onclick="reqParam(this, undefined, [\'PROBE\'])">Subscribe</a></small></h5>'
        + '<div class="card-body">'
        + SensorsTable(sensors).prop('outerHTML')
        + '</div>'
        + '</div>';
    $('#printcard').append(sensorCard);

    var pumps = $.grep(data.params.OBJLIST, function (elem, idx) {
        return elem.params.OBJTYP === 'PUMP';
    });

    var pumpsCard = '<div class="card m-1">'
        + '<h5 class="card-header bg-info">Pumps<small class="float-sm-right">'
        + '<a href="#!" class="btn btn-link p-0" onclick="reqParam(this, undefined, [\'STATUS\'])">Subscribe</a></small></h5>'
        + '<div class="card-body">'
        + PumpsTable(pumps).prop('outerHTML')
        + '</div>'
        + '</div>';
    $('#printcard').append(pumpsCard);

    var heaters = $.grep(data.params.OBJLIST, function (elem, idx) {
        return elem.params.OBJTYP === 'HEATER' && elem.objnam !== 'HXSLR';
    });

    var heatersCard = '<div class="card m-1">'
        + '<h5 class="card-header bg-info">Heaters<small class="float-sm-right">'
        + '<a href="#!" class="btn btn-link p-0" onclick="reqParam(this, undefined, [\'HTMODE\'])">Subscribe</a></small></h5>'
        + '<div class="card-body">'
        + HeatersTable(heaters).prop('outerHTML')
        + '</div>'
        + '</div>';
    $('#printcard').append(heatersCard);

    var modules = $.grep(data.params.OBJLIST, function (elem, idx) {
        return elem.params.OBJTYP === 'MODULE';
    });

    $(modules).each(function () {
        var circuits = $.grep(this.params.CIRCUITS, function (elem, idx) {
            return elem.params.OBJTYP === 'CIRCUIT';
        });

        var moduleCard = '<div class="card m-1">'
            + '<h5 class="card-header bg-info">Module<small class="float-sm-right">'
            + '<a href="#!" class="btn btn-link p-0" onclick="reqParam(this, undefined, [\'STATUS\'])">Subscribe</a></small></h5>'
            + '<div class="card-body">'
            + CircuitsTable(circuits).prop('outerHTML')
            + '</div>'
            + '</div>';
        $('#printcard').append(moduleCard);
    });
}

function SensorsTable(data) {
    var table = $('<table>').addClass('table');

    var thead = $('<thead>')
    thead.append($('<tr>').append('<th>Name</th><th>Reading</th>'));
    table.append(thead);

    var tbody = $('<tbody>');
    $(data).each(function () {
        tbody.append($('<tr id="' + this.objnam + '">').append('<td>' + this.params.SNAME + '</td><td></td>'));
    });
    table.append(tbody);

    return table;
}

function PumpsTable(data) {
    var table = $('<table>').addClass('table');

    var thead = $('<thead>')
    thead.append($('<tr>').append('<th>Name</th><th>State</th>'));
    table.append(thead);

    var unique = data.filter(function (elem, index, self) {
        return index === self.findIndex(function (e) {
            return (e.objnam === elem.objnam);
        });
    });

    var tbody = $('<tbody>');
    $(unique).each(function () {
        tbody.append($('<tr id="' + this.objnam + '">').append('<td>' + this.params.SNAME + '</td><td></td>'));
    });
    table.append(tbody);

    return table;
}

function HeatersTable(data) {
    var table = $('<table>').addClass('table');

    var thead = $('<thead>')
    thead.append($('<tr>').append('<th>Name</th><th>State</th>'));
    table.append(thead);

    var tbody = $('<tbody>');
    $(data).each(function () {
        tbody.append($('<tr id="' + this.objnam + '">').append('<td>' + this.params.SNAME + '</td><td></td>'));
    });
    table.append(tbody);

    return table;
}

function CircuitsTable(data) {
    var table = $('<table>').addClass('table');

    var thead = $('<thead>')
    thead.append($('<tr>').append('<th>Name</th><th>State</th><th>Actions</th>'));
    table.append(thead);

    var tbody = $('<tbody>');
    $(data).each(function () {
        var btns = '<a href="#!" class="btn btn-primary btn-sm m-1" onclick="setParam(this, \'' + this.objnam + '\', \'STATUS\', \'ON\')">On</a>';
        btns += '<a href="#!" class="btn btn-primary btn-sm m-1" onclick="setParam(this, \'' + this.objnam + '\', \'STATUS\', \'OFF\')">Off</a>';
        tbody.append($('<tr id="' + this.objnam + '">').append('<td>' + this.params.SNAME + '</td><td></td><td>' + btns + '</td>'));
    });
    table.append(tbody);

    return table;
}