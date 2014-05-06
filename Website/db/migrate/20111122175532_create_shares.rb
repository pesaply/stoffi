# -*- encoding : utf-8 -*-
class CreateShares < ActiveRecord::Migration
  def change
    create_table :shares do |t|
      t.string :object
      t.integer :user_id
      t.integer :playlist_id
      t.string :message
      t.integer :song_id

      t.timestamps
    end
  end
end
