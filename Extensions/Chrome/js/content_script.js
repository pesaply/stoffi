function parseLinks()
{
	if (!$parseYouTube && !$parseSoundCloud)
		return;
		
	var links = document.querySelectorAll("a");
	
	for (var i  = 0; i < links.length; ++i)
		if (shouldRewrite(links[i].href))
			links[i].href = rewrite(links[i].href);
}

updateRewriteSettings(function()
{
	parseLinks();
	document.addEventListener("DOMSubtreeModified", function() { parseLinks(); }, true);
});