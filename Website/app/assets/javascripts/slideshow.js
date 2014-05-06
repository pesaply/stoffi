var slideTimeouts = new Array();
var slideDuration = 1000;
var slideDelay = 10000;
var numSlides = 10;

var slideNames = [
	"Streaming", "Interface", "Playlist from search", "Remote", "Formats",
	"Save the world", "Social", "Focus", "Cloud", "Bookmarks"
];

/*
 * Will start a slideshow.
 *
 * Changes between divs with IDs named "slideX" where X is an integer.
 *
 * param @startPosition
 *   The index of the slide to start at.
 */
function slideshow(startPosition)
{
	stopSlideshow();
	startPosition = typeof startPosition !== 'undefined' ? startPosition : 0;
	for (i=0; i < numSlides; i++)
	{
		index = ((i + startPosition) % numSlides) + 1;
		start = i * (slideDuration * 2 + slideDelay);
		stop  = start + slideDuration + slideDelay;
		slideTimeouts.push(setTimeout("$('#slide"+index+"').fadeIn("+slideDuration+");", start));
		slideTimeouts.push(setTimeout("$('#slide"+index+"').fadeOut("+slideDuration+");", stop));
	}
	slideTimeouts.push(setTimeout("slideshow("+startPosition+");", numSlides * (slideDuration * 2 + slideDelay)));
}

function stopSlideshow()
{
	for (var i=0; i < slideTimeouts.length; i++)
	{
		clearTimeout(slideTimeouts[i]);
	}
	slideTimeouts = [];
}

function jumpToSlide(number)
{
	trackEvent('Slideshow', 'Jump to ' + slideNames[number-1]);
	stopSlideshow();
	
	for (i=1; i < numSlides+1; i++)
	{
		if (i == number)
			$('#slide'+i).fadeIn(slideDuration);
		else
			$('#slide'+i).hide();
	}
	
	slideshow(number-1);
}