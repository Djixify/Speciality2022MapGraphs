var server = "localhost:44342";

var token = "024b9d34348dd56d170f634e067274c6";
var datasets = ["vejmanhastigheder", "geodanmark60"];
var dataset = "vejmanhastigheder";
var width = 1280;
var height = 720;

var debug = true;

$(document).ready(async function(){
    var startX = 0;
    var startY = 0;
    var offsetX = 0;
    var offsetY = 0;
    var dragging = false;

    

    console.log("https://" + server + "/Map/startsession/token=" + token + ";dataset=" + dataset + ";width=" + width + ",height=" + height)
    fetch("https://" + server + "/Map/startsession/token=" + token + ";dataset=" + dataset + ";width=" + width + ",height=" + height, {method: "POST"});
    moveMap(0,0)
    
    $('#map').css('width', width);
    $('#map').css('height', height);
    $('#map').css('background-size', width + "px " + height + "px");

    $('#map').mousedown(function(e) {
       console.log("Started dragging")
       var bounds = $("#map").get(0).getBoundingClientRect()
       offsetX = e.clientX
       offsetY = e.clientY
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
        console.log("Stopped dragging")
        moveMap(-(e.clientX - startX), (e.clientY - startY));
        dragging = false;
    });
});

function zoominclicked() {
    fetch("https://" + server + "/Map/zoom=2.0", {method: "POST"})
        .finally(_ => 
        {
            console.log("updated image"); 
            $('#map').css('background-image', 'url(\"https://' + server + '/Map?t=' + (new Date().getTime()) + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}

function zoomoutclicked() {
    fetch("https://" + server + "/Map/zoom=0.5", {method: "POST"})
        .finally(_ => 
        {
            console.log("updated image"); 
            $('#map').css('background-image', 'url(\"https://' + server + '/Map?t=' + (new Date().getTime()) + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}

function toggledebug() {
    debug = !debug
    fetch("https://" + server + "/Map/debug=" + debug.toString(), {method: "POST"})
        .finally(_ => {console.log("updated image"); 
        $('#map').css('background-image', 'url(\"https://' + server + '/Map?t=' + (new Date().getTime()) + '\")');
        $('#map').css('background-position-x', 0);
        $('#map').css('background-position-y', 0);})
}

function moveMap(moveX, moveY) {
    fetch("https://" + server + "/Map/move=" + moveX.toString() + "," + moveY.toString(), {method: "POST"})
        .finally(_ => 
        {
            console.log("updated image"); 
            $('#map').css('background-image', 'url(\"https://' + server + '/Map?t=' + new Date().getTime() + '\")');
            $('#map').css('background-position-x', 0);
            $('#map').css('background-position-y', 0);
        })
}