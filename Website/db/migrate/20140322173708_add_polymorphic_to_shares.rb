class AddPolymorphicToShares < ActiveRecord::Migration
  def up
	change_table :shares do |t|
		t.references :object, :polymorphic => true
	end

	Share.all.each do |s|
		s.object_type = s.object.capitalize
		if s.object == 'song'
			s.object_id = s.song_id
		else
			s.object_id = s.playlist_id
		end
		s.save
	end

	remove_column :shares, :object
	remove_column :shares, :song_id
  end

  def down
	add_column :shares, :object, :string
	add_column :shares, :song_id, :integer
	
	Share.all.each do |s|
		s.object = s.object_type.downcase
		if s.object == 'song'
			s.song_id = s.object_id
		else
			s.playlist_id = s.object_id
		end
		s.save
	end

    remove_column :shares, :object_id
    remove_column :shares, :object_type
  end
end
