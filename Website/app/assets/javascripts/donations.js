function refresh0() { refreshSliders(0); }
function refresh1() { refreshSliders(1); }
function refresh2() { refreshSliders(2); }

$(function()
{
	$(document).ready(function()
	{
		$('[data-element="donation-revoke-link"]').each(function(index)
		{
			var revoke_e = $(this);
			var id = $(this).attr('data-objectid');
			var status_e = $('[data-field="donation-'+id+'-status"]');
			if (status_e.length > 0)
			{
				var status = $(status_e).attr('data-status');
				$('#revoke-link-'+id)
				.bind('ajax:beforeSend', function()
				{
					$(status_e).text(trans[locale]["donations.status.revoked"].toLowerCase());
					$(revoke_e).hide();
				})
				.bind('ajax:error', function()
				{
					$(status_e).text(trans[locale]["donations.status."+status].toLowerCase());
					$(revoke_e).show();
				});
			}
		});
	});
});

/*
$(function()
{
	$("#donations #artists .pagination a").live("click", function()
	{
		$("#artist_overlay").show();
		$("#artist_list").fadeTo('fast', 0.33);
		$.getScript(this.href, function(data, textStatus, jqxhr)
		{
			$("#artist_overlay").hide();
			$("#artist_list").fadeTo('fast', 1.0);
		});
		return false;
	});
});
*/

/*
 * Open a dialog to send money to
 * an artist.
 *
 * param @aid
 *   The ID of the artist
 */
function donate(aid)
{
	trackEvent('Donation', 'Dialog');
	$('#dialog').css('top', (50 + jQuery(window).scrollTop())+"px");
	$('#header').css('position', 'absolute');
	$('#dialog').addClass("blue box");
	$('#dialog').addClass("loading");
	$('#dialog').html("<img src='/assets/gfx/ajax_loading.gif'/></img>");
	addBlur(function()
	{
		if (!$('#dimmer').is(':visible'))
			$('#dimmer').fadeIn(500);
			
		if (!$('#dialog').is(':visible'))
			$('#dialog').fadeIn(200);
		
		$.ajax({
			url: "/donations/new",
			data: { artist_id: aid, ajax: true },
			success: function(html)
			{
				$('#dialog').removeClass("loading");
				$('#dialog').html(html);
			}
		});
	});
}

jQuery(window).scroll(function()
{
	var d = $('#dialog');
	var m = parseInt(d.css('margin-top'));
	if (d.is(':visible') && m >= 0)
	{
		var margin = 50;
		var pos = $('#dialog').position();
		var top = jQuery(window).scrollTop() + margin;
		if (top < pos.top)
			$('#dialog').css('top', top+"px");
	}
});

function refreshLabels()
{	
	v0 = values[0].toFixed(0);
	v1 = values[1].toFixed(0);
	v2 = values[2].toFixed(0);
	$("#0_label").html(v0);
	$("#1_label").html(v1);
	$("#2_label").html(v2);
	
	s = "a:"+donate_artist_id+";r:"+$("#return").val()+";0:"+v0+";1:"+v1+";2:"+v2;
	if (user_id > 0) s += ";u:" + user_id;
	$("#pp_param").val(s);
}

function refreshSliders(index)
{
	// calculate total change of currently sliding slider
	current_value = $("#"+index+"_slider").slider("value");
	old_value = values[index];
	pot = old_value - current_value;
	
	while (pot != 0)
	{
		// find number of sliders to distribute over
		j=0;
		for (i=0; i<3; i++)
		{
			s = $("#"+i+"_slider").slider("value");
			if (i != index && ((pot > 0 && s < 100) || (pot < 0 && s > 0)))
				j++;
		}
	
		if (j <= 0) break;
		
		// distribute pot over sliders
		console.log("distribute " + pot + " over " + j + " sliders");
		diff = pot / j;
		pot = 0;
		for (i=0; i < 3; i++)
		{
			e = $("#"+i+"_slider");
			s = e.slider("value");
			if (i != index && ((diff > 0 && s < 100) || (diff < 0 && s > 0)))
			{
				v = values[i] + diff;
				
				if (v < 0)
				{
					pot -= 0 - v;
					v = 0;
				}
				else if (v > 100)
				{
					pot += v - 100;
					v = 100;
				}
				
				values[i] = v;
				e.slider("value", v);
			}
		}
	}
	
	values[index] = $("#"+index+"_slider").slider("value");
	
	setValues();
}

function setValues(event, ui)
{	
	v1 = Math.round(values[1]);
	v2 = Math.round(values[2]);
	v0 = 100 - v1 - v2;
	
	$("#0_slider").slider("value", v0);
	$("#1_slider").slider("value", v1);
	$("#2_slider").slider("value", v2);
	refreshLabels();
}

function toggleDistribution()
{
	e = $("#sliders");
	i = $("#dist_indicator");
	if (e.is(':visible'))
	{
		e.slideUp();
		if (i.html() == "▾")
			i.html("▸");
		else
			i.html("<img src='/assets/gfx/distribution_closed.png'/>")
	}
	else
	{
		e.slideDown();
		if (i.html() == "▸")
			i.html("▾");
		else
			i.html("<img src='/assets/gfx/distribution_open.png'/>")
	}
}

function verifyDonation()
{
	v = $("#amount").val();
	if (isNaN(v))
	{
		$("#notice").html(trans[locale]["donate.invalid_amount"]);
		$("#notice").show();
		return false;
	}
	else if (Number(v) < 0.99)
	{
		$("#notice").html(trans[locale]["donate.too_little"]);
		$("#notice").show();
		return false;
	}
	else if ($("#item_name").val().replace(/^\s+|\s+$/g, '').length <= 0)
	{
		$("#notice").html(trans[locale]["donate.missing_artist"]);
		$("#notice").show();
		return false;
	}
}