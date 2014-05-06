# -*- encoding : utf-8 -*-
class CreateLanguages < ActiveRecord::Migration
  def change
    create_table :languages do |t|
      t.string :native_name
      t.string :english_name
      t.string :iso_tag
      t.string :ietf_tag

      t.timestamps
    end
  end
end
