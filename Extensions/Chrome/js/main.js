function saveOptions()
{
	var parseYouTube = document.getElementById("parseYouTube");
	var parseSoundCloud = document.getElementById("parseSoundCloud");
	
	localStorage["parseYouTube"] = parseYouTube.checked;
	localStorage["parseSoundCloud"] = parseSoundCloud.checked;
}

function loadOptions()
{
	var parseYouTube = document.getElementById("parseYouTube");
	var parseSoundCloud = document.getElementById("parseSoundCloud");
	
	parseYouTube.checked = localStorage["parseYouTube"] === "true";
	parseSoundCloud.checked = localStorage["parseSoundCloud"] === "true";
}

if (localStorage["parseYouTube"] === undefined)
	localStorage["parseYouTube"] = "true";
if (localStorage["parseSoundCloud"] === undefined)
	localStorage["parseSoundCloud"] = "true";

loadOptions();
document.getElementById("parseYouTube").onclick = function()
{
	saveOptions();
};
document.getElementById("parseSoundCloud").onclick = function()
{
	saveOptions();
};

document.title = chrome.i18n.getMessage("title");
$("#title").text(chrome.i18n.getMessage("title"));
$("#open").text(chrome.i18n.getMessage("open"));