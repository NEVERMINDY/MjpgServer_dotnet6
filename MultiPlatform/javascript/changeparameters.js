"use strict"
var Parameters = {
    WhetherResize: false,
    WhetherBrightness: false,
    WhetherContrast: false,
    WhetherDrawString: false,
    WidthPercent: 100,
    HeightPercent: 100,
    BrightnessValue: 0,
    ContrastValue: 0,
    playdelay: 20,
    StringToDraw: "",
};

function resetParameters() {
    document.getElementById("PWidthPercent").value = 100;
    document.getElementById("PHeightPercent").value = 100;
    document.getElementById("PBrightValue").value = 0;
    document.getElementById("PContrastValue").value = 0;
    document.getElementById("PDelayValue").value = 1;
    document.getElementById("PStringToDraw").value = "";
}

function changeParameters() {
    Parameters.WhetherResize = document.getElementById("PWidthPercent").value != 100 || document.getElementById("PHeightPercent").value != 100 ? true : false;
    Parameters.WhetherBrightness = document.getElementById("PBrightValue").value != 0 ? true : false;
    Parameters.WhetherContrast = document.getElementById("PContrastValue").value != 0 ? true : false;
    Parameters.WhetherDrawString = document.getElementById("PStringToDraw").value != "" ? true : false;
    var WhetherChangeDelay = document.getElementById("PDelayValue").value != "" ? true : false;

    if (Parameters.WhetherResize || Parameters.WhetherBrightness || Parameters.WhetherContrast || WhetherChangeDelay || Parameters.WhetherDrawString) {
        Parameters.WidthPercent = document.getElementById("PWidthPercent").value;
        Parameters.HeightPercent = document.getElementById("PHeightPercent").value;
        Parameters.BrightnessValue = document.getElementById("PBrightValue").value;
        Parameters.ContrastValue = document.getElementById("PContrastValue").value;
        Parameters.StringToDraw = document.getElementById("PStringToDraw").value;
        Parameters.playdelay = document.getElementById("PDelayValue").value;

        var changeParameterRequest = new XMLHttpRequest();
        changeParameterRequest.open("POST", "/", true);
        changeParameterRequest.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
        var data = "CHANGEPARAMETERS" + "&" +
            Parameters.WhetherResize + "&" +
            Parameters.WhetherBrightness + "&" +
            Parameters.WhetherContrast + "&" +
            Parameters.WidthPercent + "&" +
            Parameters.HeightPercent + "&" +
            Parameters.BrightnessValue + "&" +
            Parameters.ContrastValue + "&" +
            Parameters.playdelay + "&" +
            Parameters.StringToDraw;
        changeParameterRequest.send(data);
    }
};

function onInputHandler(event) {
    console.log("change:" + event.target);
}

function SetDelay(event) {
    if (event.keycode == 13) {
        changeParameters();
    }
}