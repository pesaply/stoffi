# -*- encoding : utf-8 -*-
class AddEmailToDonations < ActiveRecord::Migration
  def change
    add_column :donations, :email, :string
  end
end
