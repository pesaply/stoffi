# -*- encoding : utf-8 -*-
class CreateQueues < ActiveRecord::Migration
  def self.up
    create_table :queues do |t|
      t.integer :user_id
      t.timestamps
    end
	create_table :queues_songs, :id => false do |t|
	  t.references :queue, :song
	end
  end

  def self.down
    drop_table :queues
    drop_table :queues_songs
  end
end
