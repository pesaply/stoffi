# -*- encoding : utf-8 -*-
class AddCustomNameToUsers < ActiveRecord::Migration
  def change
    add_column :users, :custom_name, :string
  end
end
