# -*- encoding : utf-8 -*-
class Mailer < ActionMailer::Base
	default :to => "info@stoffiplayer.com"
	
	def contact(options)
		reply_with_name = "#{options[:name]} <#{options[:from]}>"
		from_with_name = "#{options[:name]} <noreply@stoffiplayer.com>"
		@message = options[:message]
		mail(:from => from_with_name, :reply_to => reply_with_name, :subject => options[:subject])
	end
end
