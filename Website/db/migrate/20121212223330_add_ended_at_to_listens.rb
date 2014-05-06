# -*- encoding : utf-8 -*-
class AddEndedAtToListens < ActiveRecord::Migration
  def change
    add_column :listens, :ended_at, :datetime

  end
end
