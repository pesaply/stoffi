# -*- encoding : utf-8 -*-
class SongsUsers < ActiveRecord::Migration
  def self.up
    create_table :songs_users, :id => false do |t|
      t.references :user, :song
    end
  end

  def self.down
	drop_table :songs_users
  end
end
