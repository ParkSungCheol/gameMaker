package com.example.test;

import androidx.room.Dao;
import androidx.room.Delete;
import androidx.room.Insert;
import androidx.room.Query;
import androidx.room.Update;

import java.util.List;

@Dao
public interface EnemyDao {

    @Insert
    void insert(Enemy enemy);

    @Update
    void update(Enemy enemy);

    @Delete
    void delete(Enemy enemy);

    @Query("SELECT * FROM enemy WHERE mapNumber = :mapNumber ORDER BY id DESC")
    List<Enemy> getAllEnemiesByMapNumber(int mapNumber);

    @Query("SELECT * FROM enemy")
    List<Enemy> getAllEnemies();
}
