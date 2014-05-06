var mobile = true;

$(window).resize(function() { adaptNavigation(); adaptRemote(); });
$(document).ready(function() { adaptNavigation(); adaptRemote(); });

/*
 * Adapt the header and navigation according to the width of the window/screen
 */
function adaptNavigation()
{
	if ($(window).width() > 500)
	{
		$('#navigation [data-hidable="yes"]').hide();
		$('#header [data-hidable="yes"]').show();
		
	
		if ($('#navigation [data-hidable="no"]').length == 0)
		{
			$('#toggleNavigation').hide();
			$('#navigation').hide();
		}
		else
			$('#toggleNavigation').show();
	}
	else
	{
		$('#navigation [data-hidable="yes"]').show();
		$('#header [data-hidable="yes"]').hide();
		$('#toggleNavigation').show();
	}
}

/*
 * Adapt the remote control by placing the device list below or to the right.
 */
function adaptRemote()
{
	if ($(window).width() > 500)
	{
		$('#devices-h-row').hide();
		$('#devices-v').show();
	}
	else
	{
		$('#devices-h-row').show();
		$('#devices-v').hide();
	}
}

/*
 *
 */
 function updateMobileObject(object_type, object_id, updated_params)
 {
	/*
	var params = JSON.parse(updated_params);
	switch (object_type)
	{
		case "configuration":
			setMediaState(object_id, params['media_state']);
			setVolumeLevel(object_id, params['volume']);
			setShuffleState(object_id, params['shuffle']);
			setRepeatState(object_id, params['repeat']);
			setNowPlaying(object_id, params['current_track']);
			break;
	}
	*/
 }

/*
 *
 */
 function createMobileObject(object_type, object_params)
 {
 }

/*
 *
 */
 function deleteMobileObject(object_type, object_id)
 {
 }
 
 $(document).click(function(sender)
 {
	n = $("#navigation");
	t = $(sender.target).closest("#navigation");
	if (n[0] != t[0] && n.css('display') != 'none' && parseInt(n.css('right'), 10) >= 0)
		toggleNavigation();
 });
 
/*
 *
 */
 function toggleNavigation()
 {	
	e = $('#navigation');
	w = e.outerWidth();
	
	// if invisble then hide and turn visible
	if (e.css("display") == 'none')
	{
		e.css('right', -w);
		e.show();
	}
	
	r = parseInt(e.css('right'), 10);
	
	e.animate({ right: (r == 0 ? -w : 0) });
 }