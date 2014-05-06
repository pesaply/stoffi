# -*- encoding : utf-8 -*-
class CreateDonations < ActiveRecord::Migration
  def change
    create_table :donations do |t|
      t.integer :artist_id
      t.decimal :artist_percentage
      t.decimal :stoffi_percentage
      t.decimal :charity_percentage
      t.decimal :amount
      t.integer :user_id

      t.timestamps
    end
  end
end
