# -*- encoding : utf-8 -*-
module SyncHelper
	def channels
		c = @channels.uniq if @channels
		c = Array.new unless c
		
		if current_user
			c << "hash_#{current_user.unique_hash}"
			c.delete("user_#{current_user.id}")
		end
		
		return c.uniq
	end
end
