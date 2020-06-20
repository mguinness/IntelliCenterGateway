// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function reqParam(btn, objname, key) {
    $(btn).addClass('disabled');
    var cmd = { "command": "RequestParamList", "objectList": [{ "objnam": objname, "keys": [key] }], "messageID": uuidv4() };
    console.log(cmd);
    connection.invoke('Request', JSON.stringify(cmd));
}

function setParam(btn, objname, state) {
    var cmd = { "command": "SetParamList", "objectList": [{ "objnam": objname, "params": { "STATUS": state } }], "messageID": uuidv4() };
    console.log(cmd);
    connection.invoke('Request', JSON.stringify(cmd));
}

function processMsg(msg) {
    if (msg.command === 'SendQuery' && msg.queryName === 'GetHardwareDefinition') {
        $.each(msg.answer[0].params.OBJLIST, function (index, value) {
            if (value.params.OBJTYP !== 'MODULE' && value.params.OBJTYP !== 'PUMP' && value.params.OBJTYP !== 'HEATER' && value.params.SUBTYP !== 'Generic') {
                var items = '';

                $.each(value.params, function (key, value) {
                    if (key !== value && key !== 'SNAME' && key !== 'HNAME' && key !== 'PARENT')
                        items += '<li>' + key + " = " + value + '</li>';
                });

                var btns = '';

                if (value.params.OBJTYP === 'SENSE')
                    btns += '<a href="#!" class="btn btn-primary m-1" onclick="reqParam(this, \'' + value.objnam + '\', \'PROBE\')">Subscribe</a>';
                else if (value.params.OBJTYP === 'CIRCUIT') {
                    if (value.params.SNAME !== 'All Lights Off')
                        btns += '<a href="#!" class="btn btn-primary m-1" onclick="setParam(this, \'' + value.objnam + '\', \'ON\')">On</a>';
                    if (value.params.SNAME !== 'All Lights On')
                        btns += '<a href="#!" class="btn btn-primary m-1" onclick="setParam(this, \'' + value.objnam + '\', \'OFF\')">Off</a>';
                }

                var html = '<div id="' + value.objnam + '" class="card m-1">'
                    + '<div class="card-body">'
                    + '<h5 class="card-title">' + value.params.SNAME + '</h5>'
                    + '<p class="card-text"><ul class="list-unstyled">' + items + '</ul></p>'
                    + '<p><span></span></p>'
                    + btns
                    + '</div>'
                    + '</div>';

                $('#printcard').append(html);
            }
        });
    }
    else if (msg.command === 'NotifyList') {
        console.log(msg);
        if (msg.objectList[0].params.hasOwnProperty('PROBE'))
            $('#' + msg.objectList[0].objnam + ' span').html('<b>Temp</b>: ' + msg.objectList[0].params.PROBE + '°');
    }
    else if (msg.command === 'SetParamList') {
        console.log(msg);
    } else {
        $("#output").append(JSON.stringify(msg) + '<br />');
    }
}
