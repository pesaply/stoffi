/*
 * Votes on a translation.
 *
 * Will send an AJAX request and change the
 * vote buttons state.
 *
 * param @value
 *   Either 1 (vote up) or -1 (vote down).
 *
 * param @transID
 *   The ID of the translation.
 */
function vote(value, transID)
{
	if (value != -1 && value != 1)
		return;
		
	eu = $("#vote_up_"+transID);
	ed = $("#vote_down_"+transID);
	
	if (value > 0)
	{
		eu.addClass('active');
		ed.removeClass('active');
	}
	else
	{
		ed.addClass('active');
		eu.removeClass('active');
	}
	
	$.ajax(
	{
		url: "/votes.json",
		dataType: "json",
		data: {"vote":{"value": value, "translation_id": transID}},
		type: "POST"
	});
}

/*
 * Creates a parameter.
 *
 * Will send an AJAX request and add a parameter
 * element.
 *
 * param @transID
 *   The ID of the translatee.
 */
function createParam(transID)
{
	name = $("#new_param_name").val();
	example = $("#new_param_example").val();
	
	if (name == "" || name == null)
		$("#new_param_error").html(t[locale]["translatee.params.missing.name"]);
	else if (example == "" || name == null)
		$("#new_param_error").html(t[locale]["translatee.params.missing.example"]);
	else
	{
		$("#parameters_list").append("<option value='0'>"+name+" ("+t[locale]["translatee.params.example_short"]+": "+example+")</option>");
		
		item = "<div class='param' id='param_0'>";
		item += "<div class='right'>";
		item += "<a onclick='removeItem(\"\", \"param_0\");'>x</a>";
		item += "</div>";
		item += "<input type='hidden' name='translatee[parameter_ids][]' value='0'/>";
		item += name+"<br/>";
		item += example+"</br>";
		item += "</div>";
		
		$("#params").append(item);
		/*
		$.ajax({
			url: '/admin/translatee_params.json',
			data: {'translatee_param':{'name':name, 'example':example}},
			type: 'POST',
			success: function(jqXHR)
			{
				alert("success!");
			}
		});
		*/
	}
}
