//https://localhost:44342
//http://localhost:8082
var server = "http://localhost:8082"; 

var token = "024b9d34348dd56d170f634e067274c6";
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

    $('#server').attr("placeholder", server);

    console.log(server + "/Map/startsession/token=" + token + ";dataset=" + dataset + ";width=" + width + ",height=" + height);
    moveMap(0,0);
    
    $('#map').css('width', width);
    $('#map').css('height', height);
    $('#map').css('background-size', width + "px " + height + "px");

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
        moveMap(-(e.clientX - startX), (e.clientY - startY));
        dragging = false;
    });
});

function updateserver() {
	var tmpserver = $('#server').val();
    fetch(tmpserver + "/Map/startsession/token=" + token + ";dataset=" + dataset + ";width=" + width + ",height=" + height, postrequestsettings).then(img =>
        {
            console.log("Successfully connected to new server: " + tmpserver);
            server = tmpserver;
            $('#server').val('');
            $('#server').attr("placeholder", server);
        }).catch(_ =>  
        {
            console.log("Failed to connect to new server: " + tmpserver);
        }).finally(_ => 
        {
            $('#map').css('background-image', 'url(\"' + server + '/Map?t=' + (new Date().getTime()) + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        });
}

function zoominclicked() {
    fetch(server + "/Map/zoom=2.0", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            $('#map').css('background-image', 'url(\"' + server + '/Map?t=' + (new Date().getTime()) + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}

function zoomoutclicked() {
    fetch(server + "/Map/zoom=0.5", putrequestsettings)
        .then(result => 
        {
            console.log("updated image"); 
            $('#map').css('background-image', 'url(\"' + server + '/Map?t=' + (new Date().getTime()) + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}

function toggledebug() {
    debug = !debug
    fetch(server + "/Map/debug=" + debug.toString(), putrequestsettings)
        .then(result => 
		{
			console.log("updated image"); 
			$('#map').css('background-image', 'url(\"' + server + '/Map?t=' + (new Date().getTime()) + '\")');
			$('#map').css('background-position-x', 0);
			$('#map').css('background-position-y', 0);
		})
}

function changedataset(){
	var x = document.getElementById("datasetselector").value;
	console.log("changed dataset " + x);
	dataset = datasets[parseInt(x)]
	fetch(server + "/Map/changedataset=" + dataset, putrequestsettings)
        .then(result => 
		{
			console.log("updated image"); 
			$('#map').css('background-image', 'url(\"' + server + '/Map?t=' + (new Date().getTime()) + '\")');
			$('#map').css('background-position-x', 0);
			$('#map').css('background-position-y', 0);
		})
}

function moveMap(moveX, moveY) {
    fetch(server + "/Map/move=" + moveX.toString() + "," + moveY.toString(), putrequestsettings)
        .then(result => 
        {
            console.log("updated image");
            $('#map').css('background-image', 'url(\"' + server + '/Map?t=' + new Date().getTime() + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}