# -*- encoding : utf-8 -*-
class ExtendApps < ActiveRecord::Migration
	def up
		change_table :client_applications do |t|
			t.string :icon_16
			t.string :icon_64
			t.string :description
			t.string :author
			t.string :author_url
		end
	end

	def down
		remove_column :client_applications, :icon_16
		remove_column :client_applications, :icon_64
		remove_column :client_applications, :description
		remove_column :client_applications, :author
		remove_column :client_applications, :author_url
	end
end
