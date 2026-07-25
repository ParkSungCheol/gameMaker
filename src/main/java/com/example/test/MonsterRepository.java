package com.example.test;

import android.app.Application;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;
import androidx.lifecycle.LiveData;

import java.util.Iterator;
import java.util.List;

public class MonsterRepository {
    private MonsterDao monsterDao;

    public MonsterRepository(Application application) {
        MonsterDatabase database = MonsterDatabase.getInstance(application);
        monsterDao = database.monsterDao();
    }

    public void insert(Monster monster) {
        new InsertMonsterAsyncTask(monsterDao).execute(monster);
    }

    public void update(Monster monster) {
        new UpdateMonsterAsyncTask(monsterDao).execute(monster);
    }

    public void delete(Monster monster) {
        new DeleteMonsterAsyncTask(monsterDao).execute(monster);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public Monster findMonsterByName(String name) {
        Iterator<Monster> it = monsterDao.getAllMonsters().iterator();
        Monster target = null;
        while(it.hasNext()) {
            Monster next = it.next();
            if(next.getName().equals(name)) {
                target = next;
                break;
            }
        }
        return target;
    }

    public List<Monster> getAllMonsters() {
        return monsterDao.getAllMonsters();
    }

    private static class InsertMonsterAsyncTask extends AsyncTask<Monster, Void, Void> {
        private MonsterDao monsterDao;

        private InsertMonsterAsyncTask(MonsterDao monsterDao){
            this.monsterDao = monsterDao;
        }
        @Override
        protected Void doInBackground(Monster... monsters) {
            monsterDao.insert(monsters[0]);
            return null;
        }
    }

    private static class UpdateMonsterAsyncTask extends AsyncTask<Monster, Void, Void> {
        private MonsterDao monsterDao;

        private UpdateMonsterAsyncTask(MonsterDao monsterDao){
            this.monsterDao = monsterDao;
        }
        @Override
        protected Void doInBackground(Monster... monsters) {
            monsterDao.update(monsters[0]);
            return null;
        }
    }

    private static class DeleteMonsterAsyncTask extends AsyncTask<Monster, Void, Void> {
        private MonsterDao monsterDao;

        private DeleteMonsterAsyncTask(MonsterDao monsterDao){
            this.monsterDao = monsterDao;
        }
        @Override
        protected Void doInBackground(Monster... monsters) {
            monsterDao.delete(monsters[0]);
            return null;
        }
    }
}
