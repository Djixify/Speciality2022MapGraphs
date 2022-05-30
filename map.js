//https://localhost:44342
//http://localhost:8082
var servers = 
    ["https://networkspeciality.dk",
     "https://localhost:44342",
     "https://localhost:5001"]
var server = servers[0];

var wmstoken = "024b9d34348dd56d170f634e067274c6";
var sessiontoken = Math.random().toString(36).substr(2);
var datasets = 
    ["vejmanhastigheder-small", 
     "geodanmark60-small",
     "vejmanhastigheder-medium", 
     "geodanmark60-medium",
     "vejmanhastigheder-large", 
     "geodanmark60-large"];
var dataset = datasets[0];
var width = 1280;
var height = 960;

var debug = true;

var getrequestsettings = { method: "GET", mode: 'cors', cache: 'no-cache'}
var postrequestsettings = { method: "POST", mode: 'cors', cache: 'no-cache'}
var putrequestsettings = { method: "POST", mode: 'cors', cache: 'no-cache'}

$(document).ready(async function(){
    var startX = 0;
    var startY = 0;
    var offsetX = 0;
    var offsetY = 0;
    var dragging = false;


    //$('#server').attr("placeholder", server);
    
    updateserver();
    $('#map').css('width', width);
    $('#map').css('height', height);
    
    updateMap();

    var timer = setTimeout(notclick, 400);
    var isclick = false;

    function notclick() {
        isclick = false;
    }

    $('#map').mousedown(function(e) {
       console.log("Started dragging");
       var bounds = $("#map").get(0).getBoundingClientRect();
       width = bounds.width;
       height = bounds.height;
       offsetX = e.clientX;
       offsetY = e.clientY;
       startX = e.clientX;
       startY = e.clientY;
       console.log(e.clientX + ' ' + e.clientY + ' ' + e.pageX + ' ' + e.pageY)
       dragging = true;
       isclick = true;
       clearTimeout(timer);
       timer = setTimeout(notclick, 400);
    });
 
    $('#map').mousemove(function(e){
        if (dragging) {
            $('#map').css('background-position-x', e.clientX - offsetX);
            $('#map').css('background-position-y', e.clientY - offsetY);
        }
        //console.log(startX, startY);
    });
  
    $('#map').mouseup(function(e) {
        console.log("Stopped dragging");
        diffX = startX - e.clientX
        diffY = startY - e.clientY
        if (isclick && (diffX*diffX + diffY*diffY) < 16) {
            selectvertex(e.pageX - $('#map').offset().left, e.pageY - $('#map').offset().top)
        }
        else {
            moveMap(-(e.clientX - startX), (e.clientY - startY));
        }
        dragging = false;
    });
});

function updateserver() {
    var tmpserver = servers[parseInt($('#server').val())];
    fetch(tmpserver + "/Map/" + sessiontoken + "/startsession/token=" + wmstoken + ";dataset=" + dataset + ";width=" + width + ",height=" + height, postrequestsettings).then(img =>
        {
            server = tmpserver;
            $('#statuslabel').text("Successfully connected to server: " + tmpserver);
            updateMapWidthHeight();
            updateExistingNetworks();
        }).catch(_ =>  
        {
            $('#statuslabel').text("Failed to connect to server: " + tmpserver);
            console.log("Failed to connect to new server: " + tmpserver);
        }).finally(_ => 
        {
            updateExistingNetworks()
            updateMap()
        });
}

function updateserver2() {
    var tmpserver = $('#server').val();
    tmpserver = parseInt(tmpserver);
    tmpserver = servers[tmpserver];
    fetch(tmpserver + "/Map/" + sessiontoken + "/startsession/token=" + wmstoken + ";dataset=" + dataset + ";width=" + width + ",height=" + height, postrequestsettings).then(img =>
        {
            $('#statuslabel').text("Successfully connected to server: " + tmpserver);
            server = tmpserver;
        }).catch(_ =>  
        {
            $('#statuslabel').text("Failed to connect to server: " + tmpserver);
        }).finally(_ => 
        {
            updateExistingNetworks()
            updateMap()
        });
}

function zoominclicked() {
    fetch(server + "/Map/" + sessiontoken + "/zoom=2.0", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            updateMap()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function zoomoutclicked() {
    fetch(server + "/Map/" + sessiontoken + "/zoom=0.5", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            updateMap()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function toggledebug() {
    debug = !debug
    fetch(server + "/Map/" + sessiontoken + "/debug=" + debug.toString(), putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            updateMap()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function toggleview() {
    fetch(server + "/Map/" + sessiontoken + "/toggledatasetrender", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            updateMap()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function updateExistingNetworks(preset) {
    fetch(server + "/Map/" + sessiontoken + "/listnetworks", getrequestsettings).then(async response =>
        {
            var networks = await response.text();
            $('#presetsselect').empty()
            if (!networks.startsWith("{")) {
                networks = networks.split(",")
                var i = 0;
                for (var network of networks) {
                    $('#presetsselect').append("<option value='" + network + "'>" + network + "</option>")
                }
                if (typeof preset !== 'undefined') {
                    $('#presetsselect').val(preset)
                }
                changedPreset()
            }      
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function changedataset(){
    var x = document.getElementById("datasetselector").value;
    console.log("changed dataset " + x);
    dataset = datasets[parseInt(x)]
    fetch(server + "/Map/" + sessiontoken + "/changedataset=" + dataset, putrequestsettings)
        .then(result => 
        {
            console.log("updated image");
            updateMap()
            updateExistingNetworks()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function changedPreset(){
    var preset = $('#presetsselect').val().toString()
    if (preset == "Custom (below)") {
        $('#networksettingsdiv *').prop('disabled', false)
    }
    else {
        $('#networksettingsdiv *').prop('disabled', true)
        fetch(server + "/Map/" + sessiontoken + "/getnetworkpreset=" + preset, getrequestsettings)
            .then(async response => {
                var presettext = await response.text();
                var splits = presettext.split(",")

                $('#networkname').val(splits[0])
                if (splits[1] == "QGIS") {
                    $('#generatorselector').val("QGIS")
                }
                else {
                    $('#generatorselector').val("Proposed")
                }
                $('#midpointradius').val(parseFloat(splits[2]))
                $('#endpointradius').val(parseFloat(splits[3]))
                $('#directioncolumn').val(splits[4])
                $('#directionforward').val(splits[5])
                $('#directionbackward').val(splits[6])
                if (splits.length >= 9) {
                    $('#weightlabel1').val(splits[7])
                    $('#weightformula1').val(decodeURIComponent(splits[8]).replaceAll("+", " ").replaceAll("]","+"))
                }
                if (splits.length >= 11) {
                    $('#weightlabel2').val(splits[9])
                    console.log(decodeURIComponent(splits[10]))
                    $('#weightformula2').val(decodeURIComponent(splits[10]).replaceAll("+", " ").replaceAll("]","+"))
                }
                if (splits.length == 13) {
                    $('#weightlabel3').val(splits[11])
                    $('#weightformula3').val(decodeURIComponent(splits[12]).replaceAll("+", " ").replaceAll("]","+"))
                }
                updateNetworkstats()
            })
    }
}

function updateNetworkstats() {
    fetch(server + "/Map/" + sessiontoken + "/networkstats", getrequestsettings)
        .then(async response => {
            var statstext = await response.text();
            if (statstext != ''){
                statstext = statstext.replaceAll('\r\n', '<br>')
                $('#networkstats').html(statstext)
            }
        });
}

function moveMap(moveX, moveY) {
    fetch(server + "/Map/" + sessiontoken + "/move=" + moveX.toString() + "," + moveY.toString(), putrequestsettings)
        .then(result => 
        {
            $('#statuslabel').text("Status: Successfully connected to new server: " + server);
            console.log("updated image");
            updateMap()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function selectvertex(x, y) {
    fetch(server + "/Map/" + sessiontoken + "/selectvertex=" + Math.round(x).toString() + "," + Math.round(y).toString(), putrequestsettings)
        .then(result => 
        {
            $('#statuslabel').text("Status: Successfully connected to new server: " + server);
            console.log("updated image");
            updateMap()
            updateNetworkstats()
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

function updateMap() {
    fetch(server + "/Map/" + sessiontoken, getrequestsettings).then(response => {
        console.log(response);
    })
    .catch(err => {
        console.log("Failed " + err)
        updateserver();
    });
    $('#map').css('background-image', 'url(\"' + server + "/Map/" + sessiontoken + '?t=' + new Date().getTime() + '\")');
    $('#map').css('background-position-x', 0);
    $('#map').css('background-position-y', 0);
}

function updateMapWidthHeight() {
    fetch(server + "/Map/" + sessiontoken + "/mapsize", getrequestsettings).then(async response => 
        {
            console.log(response);
            var widthheight = await response.text()
            widthheight = widthheight.split(",");
            $('#map').css('width', widthheight[0] + "px");
            $('#map').css('height', widthheight[1] + "px");
            $('#bodydiv').css('max-width', widthheight[0] + "px");
            $('#bodydiv').css('max-height', widthheight[1] + "px");
            $('#map').css('background-size', widthheight[0] + "px " + widthheight[1] + "px");
        })
        .catch(err => {
            console.log("Failed " + err)
        });
}

async function generateclicked() {
    var name = $('#networkname').val()
    if (name == "") {
        $('#networksettingtext').text("Name cannot be empty")
        return;
    }
    var generator = $('#generatorselector').val()
    var midpointradius = $('#midpointradius').val().toString()
    if (midpointradius == "") {
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Midpoint radius cannot be empty")
        return;
    }
    var endpointradius = $('#endpointradius').val().toString()
    if (endpointradius == "") {
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Endpoint radius cannot be empty")
        return;
    }
    var directioncol = $('#directioncolumn').val()
    if (directioncol != "") {
        var response = await fetch(server + "/Map/" + sessiontoken + "/validatecolumn=" + directioncol, getrequestsettings)
        var text = await response.text();
        if (text != "Success") {
            $('#networksettingtext').css('color', 'red');
            $('#networksettingtext').text(text)
            return;
        }
    }
    var directionforward = $('#directionforward').val()
    var directionbackward = $('#directionbackward').val()

    var label1 = $('#weightlabel1').val()
    var label2 = $('#weightlabel2').val()
    var label3 = $('#weightlabel3').val()

    var formula1 = $('#weightformula1').val()
    var formula2 = $('#weightformula2').val()
    var formula3 = $('#weightformula3').val()

    if (formula1 == "" && label1 == "" && formula2 == "" && label2 == "" && formula3 == "" && label3 == "") {
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("No weights specified, atleast one should be present")
        return;
    }

    if (formula1 != "" && label1 == ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("First weight does not have a label along with its formula")
        return;
    }
    if (formula2 != "" && label2 == ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Second weight does not have a label along with its formula")
        return;
    }
    if (formula3 != "" && label3 == ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Third weight does not have a label along with its formula")
        return;
    }

    if (formula1 == "" && label1 != ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("First weight does not have a formula along with its label")
        return;
    }
    if (formula2 == "" && label2 != ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Second weight does not have a formula along with its label")
        return;
    }
    if (formula3 == "" && label3 != ""){
        $('#networksettingtext').css('color', 'red');
        $('#networksettingtext').text("Third weight does not have a formula along with its label")
        return;
    }
    
    var weights = ""
    if (formula1 != "") {
        var response = await fetch(server + "/Map/" + sessiontoken + "/validateformula='" + encodeURIComponent(formula1.replaceAll("+", "]")) + "'", getrequestsettings)
        var text = await response.text()
        if (text.startsWith("Success parsing:")) {
            weights = weights + ";" + label1 + "," + encodeURIComponent(formula1.replaceAll("+", "]"))
        }
        else{
            $('#networksettingtext').css('color', 'red');
            $('#networksettingtext').text(text)
            return;
        }
    }

    if (formula2 != "") {
        var response = await fetch(server + "/Map/" + sessiontoken + "/validateformula='" + encodeURIComponent(formula2.replaceAll("+", "]")) + "'", getrequestsettings)
        var text = await response.text()
        if (text.startsWith("Success parsing:")) {
            weights = weights + ";" + label2 + "," + encodeURIComponent(formula2.replaceAll("+", "]"))
        }
        else{
            $('#networksettingtext').css('color', 'red');
            $('#networksettingtext').text(text)
            return;
        }
    }

    if (formula3 != "") {
        var response = await fetch(server + "/Map/" + sessiontoken + "/validateformula='" + encodeURIComponent(formula3.replaceAll("+", "]"))  + "'", getrequestsettings)
        var text = await response.text()
        if (text.startsWith("Success parsing:")) {
            weights = weights + ";" + label3 + "," + encodeURIComponent(formula3.replaceAll("+", "]"))
        }
        else{
            $('#networksettingtext').css('color', 'red');
            $('#networksettingtext').text(text)
            return;
        }
    }

    weights = weights.substring(1);

    $('#networksettingtext').css('color', 'yellow');
    $('#networksettingtext').text("Starting generating, follow progress on right...")

    var url = ""
    if (directioncol == "" || directionforward == "" || directionbackward == "") {
        url = server + "/Map/" + sessiontoken + "/generatenetworksimple/" + generator + "/name=" + name + ";endpointtolerance=" + endpointradius + ";midpointtolerance=" + midpointradius + ";weights=" + weights;
    }
    else {
        url = server + "/Map/" + sessiontoken + "/generatenetwork/" + generator + "/name=" + name + ";endpointtolerance=" + endpointradius + ";midpointtolerance=" + midpointradius + ";directioncolumn=" + directioncol + ",forwardsval=" + directionforward + ",backwardsval=" + directionbackward + ";weights=" + weights;
    }
    // /Map/testuser/generatenetwork/QGIS/name=Testnetwork;endpointtolerance=2.5;midpointtolerance=2.5;directioncolumn=dawd,forwardsval=wadwa,backwardsval=awd;weights=hello%2Cworld
    console.log("Generating network using parameters: " + url);
     fetch(url, postrequestsettings).then(async response => {
        if (response.status == 404) {
            $('#networksettingtext').css('color', 'red');
            $('#networksettingtext').text("Url could not be generated for webservice")
        }
        else {
            $('#networksettingtext').css('color', 'green');
            $('#networksettingtext').text("Network generated!")
            updateExistingNetworks(name)
        }
    }).catch(async response => {
        console.log("Failed network generation successfully")
    })
}

function validateFormula(formulainput) {
    var text = $(formulainput).val()
    if (text != "") {
        fetch(server + "/Map/" + sessiontoken + "/validateformula='" + encodeURIComponent(text.replaceAll("+", "]")) + "'", getrequestsettings)
        .then(async response => {
            var respmessage = await response.text()
            $('#formulaparsing').text(respmessage)
        })
        .catch(err => {
            console.log("Failed " + err)
        });
    }
}