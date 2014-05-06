# -*- encoding : utf-8 -*-
class AddAnalyzedAtToSongs < ActiveRecord::Migration
  def change
    add_column :songs, :analyzed_at, :datetime
  end
end
