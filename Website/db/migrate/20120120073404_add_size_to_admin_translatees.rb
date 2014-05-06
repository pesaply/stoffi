# -*- encoding : utf-8 -*-
class AddSizeToAdminTranslatees < ActiveRecord::Migration
  def change
    add_column :admin_translatees, :size, :string
  end
end
