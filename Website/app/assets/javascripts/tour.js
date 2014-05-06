var tourPos = 0;
var changingTourSlide = false;
var tourSlides = [
	'youtube', 'sources', 'playlists', 'interface',
	'shortcuts', 'bookmarks', 'jumplists', 'formats',
	'focus', 'settings', 'history', 'backup', 'quickedit',
	'equalizer', 'generator'
];

function nextSlide()
{
	if (changingTourSlide) return;
	trackEvent('Tour', 'Next at ' + tourSlides[tourPos]);
	tourPos = (tourPos + 1) % tourSlides.length;
	updateSlide();
}

function prevSlide()
{
	if (changingTourSlide) return;
	trackEvent('Tour', 'Previous at ' + tourSlides[tourPos]);
	tourPos = (tourPos - 1) % tourSlides.length;
	if (tourPos < 0) tourPos = (tourSlides.length + tourPos) % tourSlides.length;
	updateSlide();
}

function updateSlide(animate)
{
	animate = (typeof animate === "undefined") ? true : animate;

	slide = tourSlides[tourPos];
	img = '/assets/' + locale + '/tour/' + slide + '.png';
	title = trans[locale]['tour.'+slide+'.title'];
	text1 = trans[locale]['tour.'+slide+'.text1'];
	text2 = trans[locale]['tour.'+slide+'.text2'];
	text = '<p>'+text1+'</p><p>'+text2+'</p>';
	
	if (animate)
	{
		s = 500;
		$("#text").fadeOut(s);
		$("#title").fadeOut(s);
		$("#image").fadeOut(s, function()
		{
			$("#image").replaceWith("<img src='"+img+"', id='image' style='display:none;'/>");
			$("#title").html(title);
			$("#text").html(text);
			$("#image").fadeIn(s);
			$("#title").fadeIn(s);
			$("#text").fadeIn(s);
		});
	}
	else
	{
		$("#image").replaceWith("<img src='"+img+"', id='image'/>");
		$("#title").html(title);
		$("#text").html(text);
	}
	
	if (tourPos != 0 || window.location.hash != "")
		window.location.hash = slide;
}

p = tourSlides.indexOf(window.location.hash.substr(1));
if (p >= 0) tourPos = p;