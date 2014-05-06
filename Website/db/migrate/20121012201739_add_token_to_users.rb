# -*- encoding : utf-8 -*-
class AddTokenToUsers < ActiveRecord::Migration
  def change
    add_column :users, :unique_token, :string
  end
end
