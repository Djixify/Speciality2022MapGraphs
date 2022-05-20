//https://localhost:44342
//http://localhost:8082
var servers = 
    ["https://networkspeciality.dk",
     "https://localhost:44342"]
var server = servers[1];

var wmstoken = "024b9d34348dd56d170f634e067274c6";
var sessiontoken = Math.random().toString(36).substr(2);
var datasets = ["vejmanhastigheder", "geodanmark60"];
var dataset = "vejmanhastigheder";
var width = 1280;
var height = 960;

var debug = true;

var getrequestsettings = { method: "GET", mode: 'no-cors', credential: 'same-origin' }
var postrequestsettings = { method: "POST", mode: 'no-cors', credential: 'same-origin' }
var putrequestsettings = { method: "POST", mode: 'no-cors', credential: 'same-origin' }

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

    var timer = setTimeout(notclick, 100);
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
       timer = setTimeout(notclick, 100);
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
        if (isclick) {
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
            console.log("Successfully connected to new server: " + tmpserver);
            server = tmpserver;
            $('#server').val('');
            $('#server').attr("placeholder", server);
            fetch(server + "/Map/" + sessiontoken + "/mapsize", getrequestsettings).then(response => 
                {
                    console.log(response);
                    var widthheight = response.text().split(",");
                    $('#map').css('width', widthheight[0]);
                    $('#map').css('height', widthheight[1]);
                    $('#map').css('background-size', widthheight[0] + "px " + widthheight[1] + "px");
                });
        }).catch(_ =>  
        {
            console.log("Failed to connect to new server: " + tmpserver);
        }).finally(_ => 
        {
            updateMap()
        });
}

function updateserver2() {
	var tmpserver = $('#server').val();
    tmpserver = parseInt(tmpserver);
    tmpserver = servers[tmpserver];
    fetch(tmpserver + "/Map/" + sessiontoken + "/startsession/token=" + wmstoken + ";dataset=" + dataset + ";width=" + width + ",height=" + height, postrequestsettings).then(img =>
        {
            $('#statuslabel').text("Status: Successfully connected to new server: " + tmpserver);
            server = tmpserver;
        }).catch(_ =>  
        {
            $('#statuslabel').text("Status: Failed to connect to new server: " + tmpserver);
        }).finally(_ => 
        {
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
}

function zoomoutclicked() {
    fetch(server + "/Map/" + sessiontoken + "/zoom=0.5", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            updateMap()
        })
}

function toggledebug() {
    debug = !debug
    fetch(server + "/Map/" + sessiontoken + "/debug=" + debug.toString(), putrequestsettings)
        .then(result => 
		{
			console.log("updated image"); 
            updateMap()
		})
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
		})
}

function moveMap(moveX, moveY) {
    fetch(server + "/Map/" + sessiontoken + "/move=" + moveX.toString() + "," + moveY.toString(), putrequestsettings)
        .then(result => 
        {
            $('#statuslabel').text("Status: Successfully connected to new server: " + server);
            console.log("updated image");
            updateMap()
        })
}

function selectvertex(x, y) {
    fetch(server + "/Map/" + sessiontoken + "/selectvertex=" + Math.round(x).toString() + "," + Math.round(y).toString(), putrequestsettings)
        .then(result => 
        {
            $('#statuslabel').text("Status: Successfully connected to new server: " + server);
            console.log("updated image");
            updateMap()
        })
}

function updateMap() {
    $('#map').css('background-image', 'url(\"' + server + "/Map/" + sessiontoken + '?t=' + new Date().getTime() + '\")');
    $('#map').css('background-position-x', 0);
    $('#map').css('background-position-y', 0);
}