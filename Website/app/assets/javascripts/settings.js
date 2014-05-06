/*
 * Updates a link setting according to a clicked checkbox.
 *
 * param @link_id
 *   The The ID of the link object.
 *
 * param @field
 *   The name of the setting to update.
 *
 * param @element
 *   The checkbox element that was clicked.
 */
function linkCheckboxClicked(link_id, field, element)
{
	e = $(element);
	c = element.checked;
	url = "/links/"+link_id+".json";
	
	$.ajax({
		url: url,
		type: 'PUT',
		data: "link["+field+"]="+c,
		error: function(jqXHR)
		{
			if (jqXHR.status != 200)
				element.checked = !c;
		}
	});
}