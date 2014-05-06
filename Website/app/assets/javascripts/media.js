/*
 * Plays or pauses media playback by sending out
 * an AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function playPause(config_id)
{
	var e = $('#play_pause_button_' + config_id);
	var s = e.attr('data-state');
	var cmd = "play";
	
	if (s == "Playing")
	{
		cmd = "pause";
		setMediaState(config_id, "Paused");
	}
	else
		setMediaState(config_id, "Playing");
		
	$.ajax(
	{
		url: "/configurations/" + config_id + "/" + cmd + ".json",
		dataType: "json",
		type: "PUT"
	});
}

/*
 * Switches to next track by sending out an
 * AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function next(config_id)
{
	$.ajax(
	{
		url: "/configurations/" + config_id + "/next.json",
		dataType: "json",
		type: "POST"
	});
}

/*
 * Switches to previous track by sending out
 * an AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function prev(config_id)
{
	$.ajax(
	{
		url: "/configurations/" + config_id + "/prev.json",
		dataType: "json",
		type: "POST"
	});
}

/*
 * Switches shuffle state by sending out an
 * AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function shuffle(config_id)
{
	e = $('#shuffle_button_' + config_id);
	s = e.attr('data-state');
	
	if (s == "Random")
		setShuffleState(config_id, "Off");
	else
		setShuffleState(config_id, "Random");
		
	sendUpdate(config_id, {"shuffle":e.attr('data-state')});
}

/*
 * Switches repeat state by sending out an
 * AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function repeat(config_id)
{
	e = $('#repeat_button_' + config_id);
	s = e.attr('data-state');
	
	if (s == "NoRepeat")
		setRepeatState(config_id, "RepeatAll");
	else if (s == "RepeatAll")
		setRepeatState(config_id, "RepeatOne");
	else
		setRepeatState(config_id, "NoRepeat");
		
	sendUpdate(config_id, {"repeat":e.attr('data-state')});
}

/*
 * Increase volume level.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function volumeUp(config_id)
{
	var e = $('#volume_level_' + config_id);
	var v = parseInt(e.val());
	v += 10;
	if (v > 100) v = 100;
	e.val(v);
	
	volume(config_id);
}

/*
 * Decrease volume level.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function volumeDown(config_id)
{
	var e = $('#volume_level_' + config_id);
	var v = parseInt(e.val());
	v -= 10;
	if (v < 0) v = 0;
	e.val(v);
	
	volume(config_id);
}

/*
 * Adjusts volume level by sending out
 * an AJAX request.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
var volume_timer = {};
function volume(config_id)
{
	var e = $('#volume_level_' + config_id);
	
	if (e.length > 0)
	{
		if (volume_timer[config_id])
			clearTimeout(volume_timer[config_id]);
		volume_timer[config_id] = setTimeout(function()
		{
			sendUpdate(config_id, {"volume":e.val()});
		}, 500);
		updateVolumeIndicator(config_id);
	}
}

/*
 * Sends an AJAX request updating a
 * configuration's parameters.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param params
 *   The parameters and their values.
 */
function sendUpdate(config_id, params)
{
	$.ajax(
	{
		url: "/configurations/" + config_id + ".json",
		dataType: "json",
		data: {"configuration": params},
		type: "PUT"
	});
}


/*
 * Sets the state of the play/pause button.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param state
 *   The state to change to.
 */
function setMediaState(config_id, state)
{
	e = $('#play_pause_button_' + config_id);
	if (e.length > 0 && state)
	{
		$('#play_pause_img_' + config_id).attr('src', playPauseImage(state));
		e.attr('data-state', state);
	}
}

/*
 * Sets the state of the shuffle button.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param state
 *   The state to change to.
 */
function setShuffleState(config_id, state)
{
	e = $('#shuffle_button_' + config_id);
	if (e.length > 0 && state)
	{
		$('#shuffle_img_' + config_id).attr('src', shuffleImage(state));
		e.attr('data-state', state);
	}
}

/*
 * Sets the state of the repeat button.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param state
 *   The state to change to.
 */
function setRepeatState(config_id, state)
{
	e = $('#repeat_button_' + config_id);
	if (e.length > 0 && state)
	{
		$('#repeat_img_' + config_id).attr('src', repeatImage(state));
		e.attr('data-state', state);
	}
}

/*
 * Sets the level of the volume slider.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param level
 *   The level to change to.
 */
function setVolumeLevel(config_id, level)
{
	var e = $('#volume_level_' + config_id);
	if (e.length > 0 && level)
		e.val(level);
	updateVolumeIndicator(config_id);
}

/*
 * Sets the label showing what's currently
 * being played.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 *
 * @param value
 *   The string describing currently playing song.
 */
function setNowPlaying(config_id, value)
{
	e = $('[data-field="configuration-' + config_id+'-now-playing"]');
	if (e.length > 0)
	{
		title = value ? value : trans[locale]["media.nothing_playing"];
		e.html(title);
	}
}


/*
 * Changes the volume indicator image
 * according to the volume slider.
 *
 * @param config_id
 *   The ID of the config to manipulate.
 */
function updateVolumeIndicator(config_id)
{
	var e = $('#volume_level_' + config_id);
	var v = parseInt(e.val());
	
	for (var i=1; i <= 5; i++)
	{
		var l = $('#level-'+i);
		if (l.length > 0)
		{
			if (v >= i*20)
				l.addClass('filled');
			else
				l.removeClass('filled');
		}
	}
}

/*
 * Returns the image for the repeat button
 * given the current repeat state.
 *
 * @param state
 *   The current repeat state.
 */
function repeatImage(state)
{
	if (state == "RepeatAll")
		return "/assets/media/repeat_all.png";
	else if (state == "RepeatOne")
		return "/assets/media/repeat_one.png";
	else
		return "/assets/media/repeat_off.png";
}

/*
 * Returns the image for the shuffle button
 * given the current shuffle state.
 *
 * @param state
 *   The current shuffle state.
 */
function shuffleImage(state)
{
	if (state == "Random")
		return "/assets/media/shuffle_on.png";
	else if (state == "MindReader")
		return "/assets/media/shuffle_smart.png";
	else
		return "/assets/media/shuffle_off.png";
}

/*
 * Returns the image for the play/pause button
 * given the current media state.
 *
 * @param state
 *   The current media state.
 */
function playPauseImage(state)
{
	if (state == "Playing")
		return "/assets/media/pause.png";
	else
		return "/assets/media/play.png";
}