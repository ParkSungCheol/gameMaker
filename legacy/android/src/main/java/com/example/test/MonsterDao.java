package com.example.test;

import androidx.room.Dao;
import androidx.room.Delete;
import androidx.room.Insert;
import androidx.room.Query;
import androidx.room.Update;

import java.lang.reflect.Array;
import java.util.Arrays;
import java.util.List;

@Dao
public interface MonsterDao {

    @Insert
    void insert(Monster monster);

    @Update
    void update(Monster monster);

    @Delete
    void delete(Monster monster);

    @Query("SELECT * FROM monster ORDER BY id DESC")
    List<Monster> getAllMonsters();
}
