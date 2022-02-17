"use strict"
var Parameters = {
    WhetherResize: false,
    WhetherBrightness: false,
    WhetherContrast: false,
    WidthPercent: 100,
    HeightPercent: 100,
    BrightnessValue: 0,
    ContrastValue: 0,
    playdelay: 20,
};

function resetParameters() {
    document.getElementById("PWidthPercent").value = 100;
    document.getElementById("PHeightPercent").value = 100;
    document.getElementById("PBrightValue").value = 0;
    document.getElementById("PContrastValue").value = 0;
    document.getElementById("PDelayValue").value = 1;
}

function changeParameters() {
    Parameters.WhetherResize = Parameters.WidthPercent != document.getElementById("PWidthPercent").value || Parameters.HeightPercent != document.getElementById("PHeightPercent").value ? true : false;
    Parameters.WhetherBrightness = Parameters.BrightnessValue != document.getElementById("PBrightValue").value ? true : false;
    Parameters.WhetherContrast = Parameters.ContrastValue != document.getElementById("PContrastValue").value ? true : false;
    var WhetherChangeDelay = Parameters.playdelay != document.getElementById("PDelayValue").value ? true : false;
    if (Parameters.WhetherResize || Parameters.WhetherBrightness || Parameters.WhetherContrast || WhetherChangeDelay) {
        Parameters.WidthPercent = document.getElementById("PWidthPercent").value;
        Parameters.HeightPercent = document.getElementById("PHeightPercent").value;
        Parameters.BrightnessValue = document.getElementById("PBrightValue").value;
        Parameters.ContrastValue = document.getElementById("PContrastValue").value;
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
            Parameters.playdelay;
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