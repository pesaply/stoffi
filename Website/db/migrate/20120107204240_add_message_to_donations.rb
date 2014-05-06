# -*- encoding : utf-8 -*-
class AddMessageToDonations < ActiveRecord::Migration
  def change
    add_column :donations, :message, :string
  end
end
