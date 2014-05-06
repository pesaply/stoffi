/*
 * Evaluates the current password and displays an indicator of its strength.
 */
function evaluatePassword()
{
	var p = $("#user_plain").val();
	
	if (p.length == 0)
	{
		$("#crack_time").hide();
		return;
	}
	
	var bots = 20000;
	var speed = 1000000000; // per second
	
	// keyspace
	var n = 0;
	if (p.match(/[a-z]/)) n += 25;
	if (p.match(/[A-Z]/)) n += 25;
	if (p.match(/[0-9]/)) n += 10;
	if (p.match(/[^a-zA-Z0-9]/)) n += 20;
	
	var c = Math.pow(n,p.length) / (bots * speed);
	
	if (c < 1)
		$("#crack_time_text").text(trans[locale]['notice.password.hint.very_short']);
	else if (c <= 60)
		$("#crack_time_text").text(hintText(c, 'seconds'));
	else if (c <= 60 * 60)
		$("#crack_time_text").text(hintText(c/60, 'minutes'));
	else if (c <= 60 * 60 * 24)
		$("#crack_time_text").text(hintText(c/(60*60), 'hours'));
	else if (c <= 60 * 60 * 24 * 7)
		$("#crack_time_text").text(hintText(c/(60*60*24), 'days'));
	else if (c <= 60 * 60 * 24 * 60)
		$("#crack_time_text").text(hintText(c/(60*60*24*7), 'weeks'));
	else if (c <= 60 * 60 * 24 * 365)
		$("#crack_time_text").text(hintText(c/(60*60*24*30), 'months'));
	else if (c <= 60 * 60 * 24 * 365 * 100)
		$("#crack_time_text").text(hintText(c/(60*60*24*365), 'years'));
	else if (c <= 60 * 60 * 24 * 365 * 100 * 100)
		$("#crack_time_text").text(hintText(c/(60*60*24*365*100), 'centuries'));
	else
		$("#crack_time_text").text(trans[locale]['notice.password.hint.very_long']);
		
	$("#crack_time").show();
}

/*
 * Returns a localized text given an amount of time and unit.
 */
function hintText(time, unit)
{
	console.log("hintText("+time+", "+unit+")");
	time = Math.round(time);
	
	unit = 'x_'+unit;
	if (unit == 'x_hours' || unit == 'x_years')
		unit = 'about_'+unit;
	
	var u = trans[locale]['datetime.distance_in_words.'+unit+'.other']
	if (time == 1)
		u = trans[locale]['datetime.distance_in_words.'+unit+'.one']
	else
	{
		u = u.replace("%{count}", time);
	}
	
	console.log("u = " + u);
	var txt = trans[locale]['notice.password.hint.text'];
	console.log(txt);
	txt = txt.replace("%{time}", u);
	return txt;
}

/*
 * Hashes the password in the form.
 * The email is used as a salt.
 *
 * param @header
 *   Whether to hash the header form or a body form
 */
function hashPasswords()
{
	u = $('#user_plain');
	uc = $('#user_plain_confirmation');
	c = $('#user_current_plain');
	
	p = $('#user_password');
	pc = $('#user_password_confirmation');
	cp = $('#user_current_password');
	
	e = $('#user_email');
	
	if (e != null && e.length != 0)
	{
		salt = e.val();
		if (u != null && u.val() != null && u.val().length != 0)
			p.val(hash(u.val() + salt, "sha256"));
		
		if (uc != null && uc.val() != null && uc.val().length != 0)
			pc.val(hash(uc.val() + salt, "sha256"));
		
		if (c != null && c.val() != null && c.val().length != 0)
			cp.val(hash(c.val() + salt, "sha256"));
	
	}
	
	// remove plain passwords before submitting so they are not sent on network
	u.attr('name', '');
	uc.attr('name', '');
	c.attr('name', '');
	
	return true;
}