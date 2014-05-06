# -*- encoding : utf-8 -*-
module UsersHelper
	def text_ads?
		current_user == nil || current_user.show_ads != "none"
	end
	
	def image_ads?
		current_user == nil || (current_user && current_user.show_ads == "all")
	end
end
