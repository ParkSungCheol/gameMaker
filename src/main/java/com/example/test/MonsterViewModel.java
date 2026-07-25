package com.example.test;

import android.app.Application;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;
import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import com.fasterxml.jackson.databind.ObjectMapper;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class MonsterViewModel extends ViewModel {
    // CRUD
    private MonsterRepository monsterRepository;
    private EnemyRepository enemyRepository;
    private PlayerRepository playerRepository;

    // Create a LiveData
    private MutableLiveData<Monster> ourNewMonster = null;
    private MutableLiveData<Monster> yourNewMonster = null;
    private MutableLiveData<List<Enemy>> enemies = null;
    private MutableLiveData<Integer> money = null;

    public MutableLiveData<Monster> getOurNewMonster() {
        if (ourNewMonster == null) {
            ourNewMonster = new MutableLiveData<Monster>();
        }
        return ourNewMonster;
    }

    public MutableLiveData<Monster> getYourNewMonster() {
        if (yourNewMonster == null) {
            yourNewMonster = new MutableLiveData<Monster>();
        }
        return yourNewMonster;
    }

    public MutableLiveData<List<Enemy>> getEnemies() {
        if (enemies == null) {
            enemies = new MutableLiveData<List<Enemy>>();
        }
        return enemies;
    }

    public MutableLiveData<Integer> getMoney() {
        if (money == null) {
            money = new MutableLiveData<Integer>();
        }
        return money;
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public synchronized void addOurNewMonster(String name) {
        ourNewMonster.postValue(monsterRepository.findMonsterByName(name));
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public synchronized void addYourNewMonster(String name) {
        yourNewMonster.postValue(monsterRepository.findMonsterByName(name));
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public Monster findMonsterByName(String name) {
        return monsterRepository.findMonsterByName(name);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void getEnemiesByMapNumber(int mapNumber) {
        enemies.postValue(enemyRepository.findEnemyByMapNumber(mapNumber));
    }

    public List<Enemy> getAllEnemies() {
        return enemyRepository.getAllEnemies();
    }

    public List<Monster> getAllMonsters() {
        return monsterRepository.getAllMonsters();
    }

    public List<Player> getAllPlayer() {
        return playerRepository.getAllPlayer();
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void clear(String name, int mapNumber) {
        playerRepository.clear(name, mapNumber);
    }

    public int setMoney(String name) {
        try {
            money.postValue(playerRepository.getPlayer(name).getMoney());
            return 0;
        } catch (Exception e) {
            return -1;
        }
    }

    public MonsterViewModel(@androidx.annotation.NonNull Application application) {
        super();
        monsterRepository = new MonsterRepository(application);
        enemyRepository = new EnemyRepository(application);
        playerRepository = new PlayerRepository(application);
    }

    public void monsterInsert(Monster monster) {
        monsterRepository.insert(monster);
    }

    public void monsterUpdate(Monster monster) {
        monsterRepository.update(monster);
    }

    public void monsterDelete(Monster monster) {
        monsterRepository.delete(monster);
    }

    public void enemyInsert(Enemy enemy) { enemyRepository.insert(enemy);    }

    public void enemyUpdate(Enemy enemy) {
        enemyRepository.update(enemy);
    }

    public void enemyDelete(Enemy enemy) {
        enemyRepository.delete(enemy);
    }
}
