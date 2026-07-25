package com.example.test;

import androidx.room.Dao;
import androidx.room.Delete;
import androidx.room.Insert;
import androidx.room.Query;
import androidx.room.Update;

import java.util.List;

@Dao
public interface PlayerDao {

    @Insert
    void insert(Player player);

    @Update
    void update(Player player);

    @Delete
    void delete(Player player);

    @Query("UPDATE player SET money = :money WHERE name = :name")
    void updateMoney(String name, int money);

    @Query("UPDATE player SET mapClear = :mapClear WHERE name = :name")
    void updateMapClear(String name, String mapClear);

    @Query("SELECT * FROM player WHERE name = :name")
    Player getPlayer(String name);

    @Query("SELECT * FROM player")
    List<Player> getAllPlayer();
}
