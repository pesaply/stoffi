# -*- encoding : utf-8 -*-
class SongRelation < ActiveRecord::Migration
  def up
    create_table :song_relations do |t|
      t.integer :song1_id
	  t.integer :song2_id
	  t.integer :user_id
	  t.integer :weight
    end
	change_table :song_relations do |t|
	  t.index :song1_id
	  t.index :song2_id
	  t.index :user_id
	end
  end

  def down
	drop_table :song_relations
  end
end
