chrome.extension.onMessage.addListener(
	function(request, sender, sendResponse)
	{
		if (request.pull == "settings")
			sendResponse(
			{
				parseYouTube: localStorage["parseYouTube"],
				parseSoundCloud: localStorage["parseSoundCloud"]
			});
	}
);

var $needClosing = new Array();

chrome.tabs.onUpdated.addListener(function(tabId, changeInfo, tab)
{
	index = $needClosing.indexOf(tabId);
	if (index >= 0)
	{
		if (tab.status == "complete")
		{
			chrome.tabs.remove(tab.id);
			$needClosing.splice(index,0);
		}
	}
	else
	{
		updateRewriteSettings();
		if (shouldRewrite(tab.url))
		{
			chrome.tabs.update(tab.id, {url:rewrite(tab.url)});
			$needClosing.push(tab.id);
		}
	}
});