# -*- encoding : utf-8 -*-
class CreateHistories < ActiveRecord::Migration
  def self.up
    create_table :histories do |t|
      t.integer :user_id
      t.timestamps
    end
	create_table :histories_songs, :id => false do |t|
	  t.references :history, :song
	end
  end

  def self.down
    drop_table :histories
	drop_table :histories_songs
  end
end
