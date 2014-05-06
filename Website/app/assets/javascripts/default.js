/*
 * Cleans a string for viewing in HTML document.
 *
 * param @str
 *   The string as it is in the database.
 */
function decode(str)
{
	if (!str || typeof(str) != "string")
		return str
	str = str.replace(/&#39;/gi, "'");
	str = str.replace(/&#34;/gi, "\"");
	str = str.replace(/&#62;/gi, ">");
	str = str.replace(/&#60;/gi, "<");
	str = str.replace(/&#38;/gi, "&");
	str = str.replace(/&apos;/gi, "'");
	str = str.replace(/&quot;/gi, "\"");
	str = str.replace(/&gt;/gi, ">");
	str = str.replace(/&lt;/gi, "<");
	str = str.replace(/&amp;/gi, "&");
	return str;
}

/*
 * Cleans a string for storing and transmitting.
 *
 * param @str
 *   The string to encode.
 */
function encode(str)
{
	if (!str || typeof(str) != "string")
		return str
	next_str = decode(str);
	while (str != next_str)
	{
		str = next_str;
		next_str = decode(str);
	}
	str = str.replace(/'/gi, "&#39;");
	str = str.replace(/"/gi, "&#34;");
	str = str.replace(/>/gi, "&#62;");
	str = str.replace(/</gi, "&#60;");
	str = str.replace(/&/gi, "&#38;");
	return str;
}

/*
 * Preloads a bunch of images.
 *
 * param @images
 *   An array of images to preload.
 */
function preloadImages(images)
{
	if (document.images)
	{
		var i=0;
		for (i=0; i < images.length; i++)
		{
			imgObj = new Image();
			imgObj.src = images[i];
		}
	}
}

/*
 * Closes the floating dialog.
 */
function closeDialog()
{
	if ($('#dialog').is(':visible'))
		$('#dialog').fadeOut(100, function()
		{
			$('#dialog').css({ 'top' : '', 'margin' : '', 'width' : '', 'height' : '' });
			$('#header').css('position', '');
			$('#dialog').removeClass('blue');
			$('#dialog').removeClass('box');
			$('#dialog').removeClass('loading');
		});
	removeBlur();
	if ($('#dimmer').is(':visible'))
		$('#dimmer').fadeOut(300);
}

/*
 * Opens the floating dialog.
 */
function openDialog(content)
{
	$('#header').css('position', 'absolute');
	$('#dialog').html(content);
	
	addBlur(function()
	{
		if (!$('#dimmer').is(':visible'))
			$('#dimmer').fadeIn(500);
		if (!$('#dialog').is(':visible'))
			$('#dialog').fadeIn(200);
	});
}

/*
 * Blurs the web page.
 */
function addBlur(callback)
{
	var elements = ['root', 'header', 'footer'];
	var options = { duration: 0 };
	for (var i=0; i < elements.length; i++)
	{
		if (i == elements.length - 1 && typeof(callback) != "undefined")
			options["complete"] = callback;
		$('#'+elements[i]).addClass('blur', options);
	}
}

/*
 * Removes blurring of the web page.
 */
function removeBlur()
{
	var elements = ['root', 'header', 'footer'];
	for (var i=0; i < elements.length; i++)
		$('#'+elements[i]).removeClass('blur', 0);
}

/*
 * Shows an image inside a floating div.
 *
 * param @image
 *   The path to the image.
 *
 * param @width
 *   The width of the image in pixels.
 *
 * param @height
 *   The height of the image in pixels.
 */
function viewImage(image, width, height)
{
	w = width + 'px';
	h = height + 'px';
	l = '-' + Math.round(width/2) + 'px';
	t = (jQuery(window).scrollTop() - Math.round(height/2)) + 'px';
	
	m = t + " 0px 0px " + l;
	
	content = "<img src='"+image+"' onclick='closeDialog();' class='interactive image'/>";
	
	$('#header').css('position', 'absolute');
	
	
	addBlur(function()
	{
		if (!$('#dimmer').is(':visible'))
			$('#dimmer').fadeIn(500);
		if (!$('#dialog').is(':visible'))
			$('#dialog').fadeIn(200);
	
		$('#dialog').css({ 'margin': m, width: w, height: h, top: '50%', 'line-height':h, 'text-align':'center' });
		//$('#dialog').addClass('loading');
		$('#dialog').html("<img src='/assets/gfx/ajax_loading.gif' onclick='closeDialog();' id='image'/>");
		$('#dialog').bind('click', closeDialog);
		
		var fatImage = new Image();
		fatImage.src = image;
		
		if (fatImage.complete)
		{
			imageLoaded(image);
			fatImage.onload = function(){};
		}
		else
		{
			fatImage.onload = function()
			{
				imageLoaded(image);
				fatImage.onload = function(){};
			}
		}
	});
}

function imageLoaded(src)
{
	$('#dialog').css({ 'line-height':'', 'text-align':'' });
	$('#dialog').unbind('click', closeDialog);
	$('#image').attr('src', src);
	$('#dialog').removeClass('loading');
}

var $hideMenuTimers = new Array();

/*
 * Shows a drop-down menu.
 *
 * param @el
 *   The id of the menu element
 */
function showMenu(el)
{
	e = $('#'+el);
	if (e.is(':visible'))
	{
		if ($hideMenuTimers[el])
		{
			clearTimeout($hideMenuTimers[el]);
			$hideMenuTimers[el] = null;
		}
	}
	else
		e.slideDown('fast');
}

/*
 * Hides a drop-down menu.
 *
 * param @el
 *   The id of the menu element
 */
function hideMenu(el)
{
	e = $('#'+el);
	$hideMenuTimers[el] = setTimeout("$('#"+el+"').slideUp('fast');", 500);
}

/*
 * Sends out an error message.
 *
 * param @message
 *   The error message.
 */
function error(message)
{
	console.log("- Somehing has gone wrong, Captain.");
	console.log("- What is it Mr. Data?");
	if (message == null)
		console.log("- Unknown, Sir.");
	else
		console.log("- I believe that " + message + ", Sir.");
}

/*
 * Submits a form if the user pressed enter.
 *
 * param @field
 *   The field that was changed
 *
 * param @song_id
 *   The event that occured
 */
function submitIfEnter(field, e, callback)
{
	var keycode;
	if (window.event) keycode = window.event.keyCode;
	else if (e) keycode = e.which;
	else return true;
	
	if (keycode == 13)
	{
		if (typeof callback !== 'undefined')
			callback();
		field.form.submit();
		return false;
	}
		return true;
}

/*
 * Removes an "item" element using a visual effect.
 *
 * param @url
 *   The URL to call with DELETE method
 *
 * param @item
 *   The ID of the element
 *
 * param @event
 *   The event
 */
function removeItem(url, item, event, collection)
{
	event.stopPropagation();
	e = $('[data-object="'+item+'"]');
	
	if (confirm(trans[locale]['confirm']))
	{
		e.slideUp('slow');
		$.ajax({
			url: url,
			type: 'DELETE',
			error: function(jqXHR)
			{
				if (jqXHR.status != 200)
					e.slideDown('slow');
			},
			success: function()
			{
				e.remove();
				if ($('[data-list="'+collection+'"] li').length == 0)
					$('[data-field="no-'+collection+'"]').slideDown();
			}
		});
	}
}

/*
 * Expands a text.
 *
 * The full text should be inside a span with class 'expanded'
 * while the contracted text should be inside a span with class
 * 'contracted'. Both these spans, as well as the expander link,
 * should be children of the same div, with class 'expandable'.
 *
 * param @el
 *   The a element inside the expander span element.
 */
function expand(el)
{
	lnk = $(el).parent('.expander');
	par = lnk.parent('.expandable');
	con = par.children('.contracted');
	exp = par.children('.expanded');
	lnk.html("&nbsp;");
	con.hide();
	exp.show();
}

var $searchFocusTimers = new Array();

function showMenu(el)
{
	e = $('#'+el);
	if (e.is(':visible'))
	{
		if ($hideMenuTimers[el])
		{
			clearTimeout($hideMenuTimers[el]);
			$hideMenuTimers[el] = null;
		}
	}
	else
		e.slideDown('fast');
}

/*
 * Makes a search box wide and opaque after a delay.
 *
 * param @el
 *   The search box.
 */
function searchFocus(el)
{
	if ($searchFocusTimers[el])
	{
		clearTimeout($searchFocusTimers[el]);
		$searchFocusTimers[el] = null;
	}
	$searchFocusTimers[el] = setTimeout("doSearchFocus('" + el + "');", 100);
}

/*
 * Makes a search box wide and opaque.
 *
 * param @el
 *   The search box.
 */
function doSearchFocus(el)
{
	e = $("#"+el);
	e.animate(
	{
		backgroundColor: "#FFFFFF",
		width: 160
	}, 200, function()
	{
		e.addClass('active-search');
	});
}

/*
 * Makes a search box small and transparent after a delay.
 *
 * param @el
 *   The search box.
 */
function searchBlur(el)
{
	if ($searchFocusTimers[el])
	{
		clearTimeout($searchFocusTimers[el]);
		$searchFocusTimers[el] = null;
	}
	$searchFocusTimers[el] = setTimeout("doSearchBlur('" + el + "');", 300);
}

/*
 * Makes a search box small and transparent.
 *
 * param @el
 *   The search box.
 */
function doSearchBlur(el)
{
	e = $("#"+el);
	e.val("");
	e.removeClass('active-search');
	e.animate(
	{
		backgroundColor: "rgba(255,255,255,0.2)",
		width: 70
	}, 500);
}

/*
 * Tracks an event.
 *
 * param @category
 *   The category of the event.
 *
 * param @action
 *   The action of the event.
 *
 * param @label
 *   The label of the event (optional).
 *
 * param @value
 *   The value of the event (optional, integer).
 *
 * param @interactive
 *   If false then the event can occur in a bounce (optional).
 *
 */
function trackEvent(category, action, label, value, interactive)
{
	label = typeof label !== 'undefined' ? label : null;
	value = typeof value !== 'undefined' ? value : null;
	interactive = typeof interactive !== 'undefined' ? interactive : true;
	
	if (typeof _gaq !== 'undefined')
		_gaq.push(['_trackEvent', category, action, label, value, interactive]);
}

/*
 * Tracks a social event.
 *
 * param @network
 *   The social network.
 *
 * param @action
 *   The action of the event.
 *
 * param @target
 *   The URL of the event (optional).
 *
 * param @path
 *   The path of the URL from which the event occured (optional).
 *
 */
function trackSocial(network, action, target, path)
{
	target = typeof target !== 'undefined' ? target : null;
	path = typeof path !== 'undefined' ? path : null;
	
	if (typeof _gaq !== 'undefined')
		_gaq.push(['_trackSocial', network, action, target, path]);
}