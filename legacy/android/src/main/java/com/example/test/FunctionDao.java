package com.example.test;

import androidx.room.Dao;
import androidx.room.Delete;
import androidx.room.Insert;
import androidx.room.Query;
import androidx.room.Update;

import java.util.List;

@Dao
public interface FunctionDao {

    @Insert
    void insert(Function function);

    @Update
    void update(Function function);

    @Delete
    void delete(Function function);

    @Query("SELECT * FROM function ORDER BY id DESC")
    List<Function> getAllFunctions();

    @Query("SELECT * FROM function WHERE name = :name")
    Function getFunctionByName(String name);
}
