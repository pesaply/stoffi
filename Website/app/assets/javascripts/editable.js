function editable_label_click(element, event)
{
	$("#"+element+"-label").hide();
	$("#"+element+"-box").show();
	$("#"+element+"-box").focus();
	$("#"+element+"-box").select();
}

function editable_box_submit(element, url, field)
{
	var oldValue = $("#"+element+"-label").text();
	var newValue = $("#"+element+"-box").val();
	$("#"+element+"-box").hide();
	$("#"+element+"-label").text(newValue);
	$("#"+element+"-label").show();
	
	if (oldValue != newValue)
	{
		newValue = newValue.replace("&","%26");
		$.ajax({
			url: url,
			type: 'PUT',
			data: field+"="+newValue,
			error: function(jqXHR)
			{
				if (jqXHR.status != 200)
				{
					$("#"+element+"-label").text(oldValue);
					$("#"+element+"-box").val(oldValue);
				}
			}
		});
	}
}

function editable_box_cancel(element)
{
	$("#"+element+"-box").hide();
	$("#"+element+"-box").val($("#"+element+"-label").text());
	$("#"+element+"-label").show();
}

function editable_box_keydown(element, url, field, event)
{
	if (event.which == 13)
		$("#"+element+"-box").blur();
	else if (event.which == 27)
		editable_box_cancel(element, url, field);
}

function editable_box_blur(element, url, field, event)
{
	if ($("#"+element+"-box").is(':visible'))
		editable_box_submit(element, url, field);
}