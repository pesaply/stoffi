# -*- encoding : utf-8 -*-
class CreateParamsTranslatees < ActiveRecord::Migration
	def up
		create_table :admin_translatees_admin_translatee_params, :id => false do |t|
			t.references :admin_translatee, :admin_translatee_param
		end
	end

	def down
		drop_table :admin_translatees_admin_translatee_params
	end
end
