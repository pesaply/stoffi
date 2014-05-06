class MakeColumnsUnique < ActiveRecord::Migration
  def change
    # remove duplicates
    remove_dups('songs_users', Song.connection)


    add_index :artists, :name, :unique => true, :name => 'by_name'
    add_index :albums_artists, [ :artist_id, :album_id ], :unique => true, :name => 'by_album_and_artist'
    add_index :artists_songs, [ :artist_id, :song_id ], :unique => true, :name => 'by_artist_and_song'
    add_index :albums_songs, [ :album_id, :song_id ], :unique => true, :name => 'by_album_and_song'
    add_index :playlists_songs, [ :playlist_id, :song_id ], :unique => true, :name => 'by_playlist_and_song'
    add_index :songs_users, [ :user_id, :song_id ], :unique => true, :name => 'by_song_and_user'
    add_index :playlists, [ :user_id, :name ], :unique => true, :name => 'by_user_and_name'
    add_index :playlist_subscribers, [ :user_id, :playlist_id ], :unique => true, :name => 'by_user_and_playlist'
  end

  def remove_dups(table, connection)
    change_table table.to_sym do |t|
      connection.execute("CREATE TABLE #{table}2 LIKE #{table}")
      connection.execute("INSERT INTO #{table}2 SELECT DISTINCT * FROM #{table}")
      connection.execute("DROP TABLE #{table}")
      connection.execute("RENAME TABLE #{table}2 TO #{table}")
    end
  end
end
