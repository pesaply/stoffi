$(function()
{
	$("#dashboard .pagination a").live("click", function()
	{
		$("#overlay").show();
		$("#donation_list").fadeTo('fast', 0.33);
		$.getScript(this.href, function(data, textStatus, jqxhr)
		{
			$("#overlay").hide();
			$("#donation_list").fadeTo('fast', 1.0);
		});
		return false;
	});
});