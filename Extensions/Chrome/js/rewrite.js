var $scid = "2ad7603ebaa9cd252eabd8dd293e9c40";
var $parseYouTube = false;
var $parseSoundCloud = true;

var $ytPattern = /http(s)?:\/\/(www\.)?youtube.com\/watch\?v=([\w-_]+).*/;
var $scTrackPattern = /http(s)?:\/\/(www\.)?soundcloud.com\/tracks\/([\w-_]+)/;
var $scPattern = /http(s)?:\/\/(www\.)?soundcloud.com\/(.+)/;

function updateRewriteSettings(callback)
{
	if (callback == null)
	{
		$parseYouTube = localStorage["parseYouTube"] !== "false";
		$parseSoundCloud = localStorage["parseSoundCloud"] !== "false";
	}
	else
	{
		chrome.extension.sendMessage({pull: "settings"}, function(response)
		{
			$parseYouTube = response.parseYouTube !== "false";
			$parseSoundCloud = response.parseSoundCloud !== "false";
			callback();
		});
	}
}

function shouldRewrite(url)
{
	if (!$parseYouTube && !$parseSoundCloud)
		return false;
		
	return (($parseYouTube && url.match($ytPattern) != null) ||
		($parseSoundCloud && url.match($scTrackPattern) != null) ||
		($parseSoundCloud && url.match($scPattern) != null));
}

function rewrite(url)
{
	if (!$parseYouTube && !$parseSoundCloud)
		return;
	
	if ($parseYouTube && url.match($ytPattern))
		return url.replace($ytPattern, "youtube://$3");
	else if ($parseSoundCloud && url.match($scTrackPattern))
		return url.replace($scTrackPattern, "soundcloud://$3");
	else if ($parseSoundCloud && url.match($scPattern))
	{
		var xhr = new XMLHttpRequest();
		xhr.open('GET', "https://api.soundcloud.com/resolve.json?client_id=" +
			$scid + "&url=" + url, false);
		xhr.send();
		if (xhr.readyState == 4 && xhr.status == 200)
		{
			var data = JSON.parse(xhr.responseText);
			if (data.kind == "track")
				return "soundcloud://"+data.id;
		}
	}
	return url;
}