"use strict"
var Parameters = {
    WhetherResize: false,
    WhetherBrightness: false,
    WhetherContrast: false,
    WidthPercent: 100,
    HeightPercent: 100,
    BrightnessValue: 0,
    ContrastValue: 0,
};

function changeParameters() {
    Parameters.WhetherResize = Parameters.WidthPercent != document.getElementById("PWidthPercent").value || Parameters.HeightPercent != document.getElementById("PHeightPercent").value ? true : false;
    Parameters.WhetherBrightness = Parameters.BrightnessValue != document.getElementById("PBrightValue").value ? true : false;
    Parameters.WhetherContrast = Parameters.ContrastValue != document.getElementById("PContrastValue").value ? true : false;
    if (Parameters.WhetherResize || Parameters.WhetherBrightness || Parameters.WhetherContrast) {
        Parameters.WidthPercent = document.getElementById("PWidthPercent").value;
        Parameters.HeightPercent = document.getElementById("PHeightPercent").value;
        Parameters.BrightnessValue = document.getElementById("PBrightValue").value;
        Parameters.ContrastValue = document.getElementById("ContrastValue").value;

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
            Parameters.ContrastValue;

        changeParameterRequest.send(data);
    }

};