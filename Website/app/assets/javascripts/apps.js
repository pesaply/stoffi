/*
 * Revokes access for an app and remove its element using a visual effect.
 *
 * param @url
 *   The revoke URL to call with POST method
 *
 * param @item
 *   The ID of the element
 *
 * param @token
 *   The token to revoke
 *
 * param @event
 *   The event
 */
function revoke(url, item, token, event)
{
	event.stopPropagation();
	e = $("#"+item);
	
	if (confirm(trans[locale]['confirm']))
	{
		e.slideUp('slow');
			
		$.ajax({
			url: url,
			type: 'POST',
			data: { token: token },
			error: function(jqXHR)
			{
				if (jqXHR.status != 200)
					e.slideDown('slow');
			}
		});
	}
}